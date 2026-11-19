using Portfolio.Application.Interfaces;
using Portfolio.Application.Models;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;

namespace Portfolio.Application.Mappers;

/// <summary>
/// DEGIRO-specific transaction mapping strategy.
/// Maps DEGIRO's EUR base currency legs, quantity sign conventions, and fees into domain Transaction entities.
/// </summary>
public class DegiroBrokerTransactionMapper : IBrokerTransactionMapper
{
    public string BrokerKey => "degiro";
    public string DisplayName => "DEGIRO";

    public Transaction MapToTransaction(
        ImportedTransactionRow row,
        Guid targetAssetId)
    {
        var txType = row.TransactionType;
        if (txType != null && txType != TransactionType.Swap)
        {
            var fromAssetId = txType.RequiresFromAsset ? targetAssetId : (Guid?)null;
            var toAssetId   = txType.RequiresToAsset   ? targetAssetId : (Guid?)null;
            var amountSpent    = txType.RequiresFromAsset ? Math.Abs(row.TotalAmount) : 0m;
            var amountReceived = txType.RequiresToAsset   ? Math.Abs(row.TotalAmount) : 0m;
            return BuildFiatTransaction(row, txType, fromAssetId, toAssetId, amountSpent, amountReceived);
        }

        Guid swapFromAssetId;
        Guid swapToAssetId;
        decimal swapAmountSpent;
        decimal swapAmountReceived;

        if (row.Quantity > 0)
        {
            // BUY: Swap EUR -> Stock
            swapFromAssetId = FiatCurrency.EUR.Id;
            swapToAssetId = targetAssetId;
            swapAmountSpent = Math.Abs(row.TotalAmount);
            swapAmountReceived = Math.Abs(row.Quantity);
        }
        else
        {
            // SELL: Swap Stock -> EUR
            swapFromAssetId = targetAssetId;
            swapToAssetId = FiatCurrency.EUR.Id;
            swapAmountSpent = Math.Abs(row.Quantity);
            swapAmountReceived = Math.Abs(row.TotalAmount);
        }

        var transaction = Transaction.CreateSwap(
            date: row.Date,
            fromAssetId: swapFromAssetId,
            toAssetId: swapToAssetId,
            amountSpent: swapAmountSpent,
            amountReceived: swapAmountReceived,
            fee: row.FeeEur ?? 0m,
            feeAssetId: FiatCurrency.EUR.Id,
            notes: $"Imported from {DisplayName} ({row.ProductName}). Order ID: {row.BrokerTransactionId}",
            brokerTransactionId: row.BrokerTransactionId
        );

        if (string.Equals(row.PriceCurrency, "USD", StringComparison.OrdinalIgnoreCase) &&
            row.FxRate.HasValue && row.FxRate.Value > 0)
        {
            // DEGIRO quotes "Tipo de cambio" as EUR -> USD (e.g. 1 EUR = 1.0635 USD).
            // Domain UsdEurExchangeRate is USD -> EUR (e.g. 1 USD = 0.9403 EUR).
            decimal usdToEurRate = 1m / row.FxRate.Value;
            transaction.UpdateExchangeRates(usdToEurRate);
        }

        return transaction;
    }

    private Transaction BuildFiatTransaction(
        ImportedTransactionRow row,
        TransactionType type,
        Guid? fromAssetId,
        Guid? toAssetId,
        decimal amountSpent,
        decimal amountReceived)
    {
        var isUsd = string.Equals(row.PriceCurrency, FiatCurrency.USD.Symbol, StringComparison.OrdinalIgnoreCase);
        var inputCurrency = isUsd ? FiatCurrency.USD : FiatCurrency.EUR;
        decimal? spotPriceUsd = isUsd ? 1m : null;
        decimal? spotPriceEur = isUsd ? null : 1m;

        var transaction = Transaction.Create(
            date: row.Date,
            transactionType: type,
            fromAssetId: fromAssetId,
            toAssetId: toAssetId,
            amountSpent: amountSpent,
            amountReceived: amountReceived,
            spotPriceUSD: spotPriceUsd,
            spotPriceEUR: spotPriceEur,
            spotPriceInputCurrency: inputCurrency,
            notes: $"Imported from {DisplayName} ({row.ProductName}). Order ID: {row.BrokerTransactionId}",
            brokerTransactionId: row.BrokerTransactionId
        );

        if (isUsd && row.FxRate.HasValue && row.FxRate.Value > 0)
        {
            decimal usdToEurRate = 1m / row.FxRate.Value;
            transaction.UpdateExchangeRates(usdToEurRate);
        }

        return transaction;
    }
}
