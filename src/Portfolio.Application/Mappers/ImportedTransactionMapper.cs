using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Application.Mappers;

/// <summary>
/// Pure mapper converting parsed external broker rows into domain Transaction entities.
/// </summary>
public static class ImportedTransactionMapper
{
    public static Transaction ToTransaction(
        ImportedTransactionRow row, 
        Guid targetAssetId, 
        string brokerName)
    {
        Guid fromAssetId;
        Guid toAssetId;
        decimal amountSpent;
        decimal amountReceived;

        if (row.Quantity > 0)
        {
            // BUY: Swap EUR -> Stock
            fromAssetId = FiatCurrency.EUR.Id;
            toAssetId = targetAssetId;
            amountSpent = Math.Abs(row.TotalEur);
            amountReceived = Math.Abs(row.Quantity);
        }
        else
        {
            // SELL: Swap Stock -> EUR
            fromAssetId = targetAssetId;
            toAssetId = FiatCurrency.EUR.Id;
            amountSpent = Math.Abs(row.Quantity);
            amountReceived = Math.Abs(row.TotalEur);
        }

        return new Transaction(
            date: row.Date,
            transactionType: TransactionType.Swap,
            fromAssetId: fromAssetId,
            toAssetId: toAssetId,
            amountSpent: amountSpent,
            amountReceived: amountReceived,
            spotPriceUSD: null,
            spotPriceEUR: null,
            fee: row.FeeEur ?? 0m,
            feeAssetId: FiatCurrency.EUR.Id,
            feeSpotPriceUSD: null,
            feeSpotPriceEUR: null,
            usdEurExchangeRate: null,
            spotPriceInputCurrency: null,
            feePriceInputCurrency: null,
            notes: $"Imported from {brokerName} ({row.ProductName}). Order ID: {row.BrokerTransactionId}",
            brokerTransactionId: row.BrokerTransactionId
        );
    }
}

