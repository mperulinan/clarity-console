using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using CsvHelper;
using Portfolio.Application.Models;
using Portfolio.Domain.Enums;

namespace Portfolio.Infrastructure.ImportParsers;

/// <summary>
/// Extracts cash transactions (deposits, withdrawals, dividends, account fees) from a DEGIRO account statement (Estado de cuenta).
/// Skips internal bank cash sweeps and trade-related execution rows.
/// </summary>
internal class DegiroAccountRowParser : IDegiroRowParser
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

        var description = csv.GetField(DegiroHeaders.Description)?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(description))
        {
            return false;
        }

        if (IsInternalSweepOrHold(description))
        {
            return false;
        }

        var orderId = csv.GetField(DegiroHeaders.OrderId)?.Trim();
        if (!string.IsNullOrWhiteSpace(orderId) && IsTradeRelated(description))
        {
            return false;
        }

        var (currency, amount) = ResolveVariation(csv, headers);
        if (!amount.HasValue || amount.Value == 0m)
        {
            return false;
        }

        var product = csv.GetField(DegiroHeaders.Product)?.Trim() ?? string.Empty;
        var isin = csv.GetField(DegiroHeaders.Isin)?.Trim() ?? string.Empty;

        var txType = ResolveTransactionType(description, amount.Value);
        var productName = string.IsNullOrWhiteSpace(product) ? description : $"{product} - {description}";

        var brokerTransactionId = !string.IsNullOrWhiteSpace(orderId)
            ? orderId
            : GenerateDeterministicId(dateTime.Value, description, isin, amount.Value);

        row = new ImportedTransactionRow(
            Date: dateTime.Value,
            ProductName: productName,
            Isin: isin,
            Exchange: string.Empty,
            Quantity: Math.Abs(amount.Value),
            PricePerUnit: 1m,
            PriceCurrency: currency,
            FxRate: null,
            FeeEur: 0m,
            TotalEur: amount.Value,
            BrokerTransactionId: brokerTransactionId,
            TransactionType: txType
        );

        return true;
    }

    private static (string Currency, decimal? Amount) ResolveVariation(CsvReader csv, string[] headers)
    {
        var variacionIndex = Array.FindIndex(headers, h => string.Equals(h?.Trim(), DegiroHeaders.Variation, StringComparison.OrdinalIgnoreCase));
        if (variacionIndex < 0)
        {
            return (DegiroHeaders.DefaultCurrency, null);
        }

        var field1 = DegiroOrderIdResolver.GetFieldByIndexSafe(csv, variacionIndex)?.Trim();
        var field2 = DegiroOrderIdResolver.GetFieldByIndexSafe(csv, variacionIndex + 1)?.Trim();

        var parsed1 = DegiroFormatParser.ParseDecimal(field1);
        if (parsed1.HasValue)
        {
            var currency = !string.IsNullOrWhiteSpace(field2) ? field2.ToUpperInvariant() : DegiroHeaders.DefaultCurrency;
            return (currency, parsed1.Value);
        }

        var parsed2 = DegiroFormatParser.ParseDecimal(field2);
        if (parsed2.HasValue)
        {
            var currency = !string.IsNullOrWhiteSpace(field1) ? field1.ToUpperInvariant() : DegiroHeaders.DefaultCurrency;
            return (currency, parsed2.Value);
        }

        return (DegiroHeaders.DefaultCurrency, null);
    }

    private static bool IsInternalSweepOrHold(string description) =>
        DegiroHeaders.InternalSweepKeywords.Any(kw => kw.Matches(description));

    private static bool IsTradeRelated(string description) =>
        DegiroHeaders.TradeRelatedKeywords.Any(kw => kw.Matches(description));

    private static TransactionType ResolveTransactionType(string description, decimal amount)
    {
        if (DegiroHeaders.DepositKeywords.Any(kw => kw.Matches(description)))
            return TransactionType.Deposit;

        if (DegiroHeaders.WithdrawalKeywords.Any(kw => kw.Matches(description)))
            return TransactionType.Withdrawal;

        if (DegiroHeaders.RewardKeywords.Any(kw => kw.Matches(description)))
            return TransactionType.Reward;

        if (DegiroHeaders.LossKeywords.Any(kw => kw.Matches(description)))
            return TransactionType.Loss;

        if (DegiroHeaders.InterestKeywords.Any(kw => kw.Matches(description)))
            return amount >= 0m ? TransactionType.Reward : TransactionType.Loss;

        return amount >= 0m ? TransactionType.Deposit : TransactionType.Loss;
    }

    private static string GenerateDeterministicId(DateTime dateTime, string description, string isin, decimal amount)
    {
        var rawKey = $"{dateTime:O}|{description.Trim().ToUpperInvariant()}|{isin.Trim().ToUpperInvariant()}|{amount:F2}";
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawKey)))[..12].ToLowerInvariant();
        return $"degiro-{hash}";
    }
}

