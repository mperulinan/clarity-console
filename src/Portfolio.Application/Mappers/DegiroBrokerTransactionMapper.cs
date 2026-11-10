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
            notes: $"Imported from {DisplayName} ({row.ProductName}). Order ID: {row.BrokerTransactionId}",
            brokerTransactionId: row.BrokerTransactionId
        );
    }
}
