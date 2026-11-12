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

        var fecha = csv.GetField(DegiroHeaders.Date);
        var hora = csv.GetField(DegiroHeaders.Time);

        var dateTime = DegiroFormatParser.ParseDateTime(fecha, hora);
        if (!dateTime.HasValue)
        {
            return false;
        }

        var productName = csv.GetField(DegiroHeaders.Product)?.Trim() ?? string.Empty;
        var isin = csv.GetField(DegiroHeaders.Isin)?.Trim() ?? string.Empty;
        var exchange = csv.GetField(DegiroHeaders.ReferenceExchange)?.Trim() ?? string.Empty;

        var quantity = DegiroFormatParser.ParseDecimal(csv.GetField(DegiroHeaders.Quantity)) ?? 0m;
        var pricePerUnit = DegiroFormatParser.ParseDecimal(csv.GetField(DegiroHeaders.Price)) ?? 0m;
        var totalEur = DegiroFormatParser.ParseDecimal(csv.GetField(DegiroHeaders.TotalEur)) ?? 0m;
        var fxRate = DegiroFormatParser.ParseDecimal(csv.GetField(DegiroHeaders.FxRate));

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
        var precioIndex = Array.FindIndex(headers, h => string.Equals(h?.Trim(), DegiroHeaders.Price, StringComparison.OrdinalIgnoreCase));
        var priceCurrency = precioIndex >= 0 ? DegiroOrderIdResolver.GetFieldByIndexSafe(csv, precioIndex + 1) : null;

        return string.IsNullOrWhiteSpace(priceCurrency) ? "EUR" : priceCurrency.ToUpperInvariant();
    }

    private static decimal? CalculateTotalFeeEur(CsvReader csv)
    {
        // Total fee combines broker execution fees and currency conversion (AutoFX) commissions
        var rawFee = DegiroFormatParser.ParseDecimal(csv.GetField(DegiroHeaders.TransactionAndExternalCostsEur));
        var rawAutoFx = DegiroFormatParser.ParseDecimal(csv.GetField(DegiroHeaders.AutoFxCommission));

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

