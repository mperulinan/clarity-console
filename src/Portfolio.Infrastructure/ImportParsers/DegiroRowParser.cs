using System.Diagnostics.CodeAnalysis;
using CsvHelper;
using Portfolio.Application.Models;

namespace Portfolio.Infrastructure.ImportParsers;

/// <summary>
/// Strategy for extracting and mapping a single DEGIRO CSV row into an <see cref="ImportedTransactionRow"/>.
/// </summary>
internal interface IDegiroRowParser
{
    bool TryParseRow(CsvReader csv, string[] headers, [NotNullWhen(true)] out ImportedTransactionRow? row);
}

/// <summary>
/// Extracts fields, calculates aggregated fees, aligns buy/sell quantity signs,
/// resolves reference currency, and derives the order ID for a single DEGIRO CSV record.
/// </summary>
internal class DegiroRowParser : IDegiroRowParser
{
    public bool TryParseRow(CsvReader csv, string[] headers, [NotNullWhen(true)] out ImportedTransactionRow? row)
    {
        row = null;

        var fecha = csv.GetField("Fecha");
        var hora = csv.GetField("Hora");

        var dateTime = DegiroFormatParser.ParseDateTime(fecha, hora);
        if (!dateTime.HasValue)
        {
            return false;
        }

        var productName = csv.GetField("Producto")?.Trim() ?? string.Empty;
        var isin = csv.GetField("ISIN")?.Trim() ?? string.Empty;
        var exchange = csv.GetField("Bolsa de referencia")?.Trim() ?? string.Empty;

        var quantity = DegiroFormatParser.ParseDecimal(csv.GetField("Número")) ?? 0m;
        var pricePerUnit = DegiroFormatParser.ParseDecimal(csv.GetField("Precio")) ?? 0m;
        var totalEur = DegiroFormatParser.ParseDecimal(csv.GetField("Total EUR")) ?? 0m;
        var fxRate = DegiroFormatParser.ParseDecimal(csv.GetField("Tipo de cambio"));

        var priceCurrency = ResolvePriceCurrency(csv, headers);
        var feeEur = CalculateTotalFeeEur(csv);
        var normalizedQuantity = NormalizeQuantitySign(quantity, totalEur);

        var brokerTransactionId = DegiroOrderIdResolver.Resolve(
            csv,
            headers,
            dateTime.Value,
            isin,
            normalizedQuantity,
            totalEur);

        row = new ImportedTransactionRow(
            Date: dateTime.Value,
            ProductName: productName,
            Isin: isin,
            Exchange: exchange,
            Quantity: normalizedQuantity,
            PricePerUnit: pricePerUnit,
            PriceCurrency: priceCurrency,
            FxRate: fxRate,
            FeeEur: feeEur,
            TotalEur: totalEur,
            BrokerTransactionId: brokerTransactionId
        );

        return true;
    }

    private static string ResolvePriceCurrency(CsvReader csv, string[] headers)
    {
        // In DEGIRO, price/monetary columns are paired with an unheadered currency column immediately after "Precio"
        var precioIndex = Array.FindIndex(headers, h => string.Equals(h?.Trim(), "Precio", StringComparison.OrdinalIgnoreCase));
        var priceCurrency = precioIndex >= 0 ? DegiroOrderIdResolver.GetFieldByIndexSafe(csv, precioIndex + 1) : null;

        return string.IsNullOrWhiteSpace(priceCurrency) ? "EUR" : priceCurrency.ToUpperInvariant();
    }

    private static decimal? CalculateTotalFeeEur(CsvReader csv)
    {
        // Total fee combines broker execution fees and currency conversion (AutoFX) commissions
        var rawFee = DegiroFormatParser.ParseDecimal(csv.GetField("Costes de transacción y/o externos EUR"));
        var rawAutoFx = DegiroFormatParser.ParseDecimal(csv.GetField("Comisión AutoFX"));

        if (!rawFee.HasValue && !rawAutoFx.HasValue)
        {
            return null;
        }

        return (rawFee.HasValue ? Math.Abs(rawFee.Value) : 0m) +
               (rawAutoFx.HasValue ? Math.Abs(rawAutoFx.Value) : 0m);
    }

    private static decimal NormalizeQuantitySign(decimal quantity, decimal totalEur)
    {
        // Align quantity sign with domain expectations (quantity > 0 for Buy, quantity < 0 for Sell)
        if (totalEur < 0 && quantity < 0)
        {
            return Math.Abs(quantity);
        }

        if (totalEur > 0 && quantity > 0)
        {
            return -Math.Abs(quantity);
        }

        return quantity;
    }
}

