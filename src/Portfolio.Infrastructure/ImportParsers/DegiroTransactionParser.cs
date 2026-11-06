using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Infrastructure.ImportParsers;

public class DegiroTransactionParser : ITransactionImportParser
{
    public string BrokerKey => "degiro";
    public string DisplayName => "DEGIRO";

    public async Task<IReadOnlyList<ImportedTransactionRow>> ParseAsync(
        Stream csvStream, 
        CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(csvStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
            BadDataFound = null
        };

        using var csv = new CsvReader(reader, config);

        if (!await csv.ReadAsync())
        {
            return Array.Empty<ImportedTransactionRow>();
        }

        csv.ReadHeader();
        var headers = csv.HeaderRecord;
        if (headers == null || !IsValidDegiroSpanishHeader(headers))
        {
            throw new InvalidOperationException("The uploaded file does not match the expected DEGIRO (Spanish) CSV format.");
        }

        var rows = new List<ImportedTransactionRow>();

        while (await csv.ReadAsync())
        {
            var fecha = csv.GetField("Fecha");
            var hora = csv.GetField("Hora");

            if (string.IsNullOrWhiteSpace(fecha))
            {
                continue;
            }

            DateTime dateTime;
            if (!string.IsNullOrWhiteSpace(hora) &&
                DateTime.TryParseExact($"{fecha} {hora}", "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsedDt))
            {
                dateTime = parsedDt;
            }
            else if (DateTime.TryParseExact(fecha, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsedDateOnly))
            {
                dateTime = parsedDateOnly;
            }
            else
            {
                dateTime = DateTime.SpecifyKind(DateTime.Parse(fecha, CultureInfo.InvariantCulture), DateTimeKind.Utc);
            }

            var productName = csv.GetField("Producto") ?? string.Empty;
            var isin = csv.GetField("ISIN") ?? string.Empty;
            var exchange = csv.GetField("Bolsa de referencia") ?? string.Empty;

            var quantity = ParseDecimal(csv.GetField("Número")) ?? 0m;
            var pricePerUnit = ParseDecimal(csv.GetField("Precio")) ?? 0m;

            // In DEGIRO, price/monetary columns are paired with an unheadered currency column immediately after
            var precioIndex = Array.FindIndex(headers, h => string.Equals(h?.Trim(), "Precio", StringComparison.OrdinalIgnoreCase));
            var priceCurrency = precioIndex >= 0 ? GetFieldByIndexSafe(csv, precioIndex + 1) : null;
            if (string.IsNullOrWhiteSpace(priceCurrency))
            {
                priceCurrency = "EUR";
            }

            var fxRate = ParseDecimal(csv.GetField("Tipo de cambio"));
            
            // Fee in EUR column: "Costes de transacción y/o externos EUR"
            var rawFee = ParseDecimal(csv.GetField("Costes de transacción y/o externos EUR"));
            decimal? feeEur = rawFee.HasValue ? Math.Abs(rawFee.Value) : null;

            var totalEur = ParseDecimal(csv.GetField("Total EUR")) ?? 0m;

            // Align quantity sign with domain expectations (quantity > 0 for Buy, quantity < 0 for Sell)
            if (totalEur < 0 && quantity < 0)
            {
                quantity = Math.Abs(quantity);
            }
            else if (totalEur > 0 && quantity > 0)
            {
                quantity = -Math.Abs(quantity);
            }

            var idOrden = ResolveBrokerTransactionId(csv, dateTime, isin, quantity, totalEur);

            rows.Add(new ImportedTransactionRow(
                Date: dateTime,
                ProductName: productName,
                Isin: isin,
                Exchange: exchange,
                Quantity: quantity,
                PricePerUnit: pricePerUnit,
                PriceCurrency: priceCurrency.ToUpperInvariant(),
                FxRate: fxRate,
                FeeEur: feeEur,
                TotalEur: totalEur,
                BrokerTransactionId: idOrden
            ));
        }

        return rows;
    }

    private static bool IsValidDegiroSpanishHeader(string[] headers)
    {
        var requiredHeaders = new[] { "Fecha", "Producto", "ISIN", "Número", "Precio", "Total EUR", "ID Orden" };
        return requiredHeaders.All(req => headers.Any(h => string.Equals(h?.Trim(), req, StringComparison.OrdinalIgnoreCase)));
    }

    private static string ResolveBrokerTransactionId(CsvReader csv, DateTime dateTime, string isin, decimal quantity, decimal totalEur)
    {
        // 1. Direct header lookup
        var id = csv.GetField("ID Orden")?.Trim();
        if (!string.IsNullOrWhiteSpace(id))
            return id;

        // 2. In real DEGIRO exports, "Total EUR" often has an unheadered column right after it,
        // which pushes the data row values one index to the right of "ID Orden".
        var headers = csv.HeaderRecord;
        if (headers != null)
        {
            var idIndex = Array.FindIndex(headers, h => string.Equals(h?.Trim(), "ID Orden", StringComparison.OrdinalIgnoreCase));
            if (idIndex >= 0)
            {
                var adjacent = GetFieldByIndexSafe(csv, idIndex + 1)?.Trim();
                if (!string.IsNullOrWhiteSpace(adjacent))
                    return adjacent;
            }
        }

        // 3. Scan the row from the right for a GUID or order-like token
        for (int col = csv.ColumnCount - 1; col >= 0; col--)
        {
            var val = GetFieldByIndexSafe(csv, col)?.Trim();
            if (!string.IsNullOrWhiteSpace(val))
            {
                if (Guid.TryParse(val, out _) || (val.Length > 8 && val.Contains('-')))
                    return val;
            }
        }

        // 4. Deterministic fallback if broker didn't provide an order ID
        return $"DEGIRO_{dateTime:yyyyMMddHHmm}_{isin}_{quantity}_{totalEur}".Replace(' ', '_');
    }

    private static decimal? ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim().Trim('"', '\'');

        // If the value contains a comma, it represents a decimal separator in Spanish DEGIRO exports
        // (e.g. "168,5600" or "-1105,33"). We must parse with Spanish culture first, because
        // InvariantCulture with NumberStyles.Any treats comma as a thousands separator.
        if (trimmed.Contains(','))
        {
            if (decimal.TryParse(trimmed, NumberStyles.Any, new CultureInfo("es-ES"), out var parsedEs))
                return parsedEs;
        }

        if (decimal.TryParse(trimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            return parsed;

        if (decimal.TryParse(trimmed, NumberStyles.Any, new CultureInfo("es-ES"), out var fallbackEs))
            return fallbackEs;

        return null;
    }

    private static string? GetFieldByIndexSafe(CsvReader csv, int index)
    {
        try
        {
            return csv.GetField(index)?.Trim();
        }
        catch
        {
            return null;
        }
    }
}
