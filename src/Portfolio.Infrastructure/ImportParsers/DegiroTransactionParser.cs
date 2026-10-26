using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Infrastructure.ImportParsers;

public class DegiroTransactionParser : ITransactionImportParser
{
    public string BrokerKey => "degiro";

    public async Task<IReadOnlyList<ImportedTransactionRow>> ParseAsync(
        Stream csvStream, 
        CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(csvStream, leaveOpen: true);
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
            var idOrden = csv.GetField("ID Orden");

            if (string.IsNullOrWhiteSpace(fecha) || string.IsNullOrWhiteSpace(idOrden))
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

            // Currency column immediately after "Precio" (index 8)
            var priceCurrency = GetFieldByIndexSafe(csv, 8);
            if (string.IsNullOrWhiteSpace(priceCurrency))
            {
                priceCurrency = "EUR";
            }

            var fxRate = ParseDecimal(csv.GetField("Tipo de cambio"));
            
            // Fee in EUR column: "Costes de transacción y/o externos EUR"
            var rawFee = ParseDecimal(csv.GetField("Costes de transacción y/o externos EUR"));
            decimal? feeEur = rawFee.HasValue ? Math.Abs(rawFee.Value) : null;

            var totalEur = ParseDecimal(csv.GetField("Total EUR")) ?? 0m;

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
                BrokerTransactionId: idOrden.Trim()
            ));
        }

        return rows;
    }

    private static bool IsValidDegiroSpanishHeader(string[] headers)
    {
        var requiredHeaders = new[] { "Fecha", "Producto", "ISIN", "Número", "Precio", "Total EUR", "ID Orden" };
        return requiredHeaders.All(req => headers.Any(h => string.Equals(h?.Trim(), req, StringComparison.OrdinalIgnoreCase)));
    }

    private static decimal? ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (decimal.TryParse(value.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            return parsed;

        // Try Spanish number format (comma as decimal separator) if invariant parse fails
        if (decimal.TryParse(value.Trim(), NumberStyles.Any, new CultureInfo("es-ES"), out var parsedEs))
            return parsedEs;

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
