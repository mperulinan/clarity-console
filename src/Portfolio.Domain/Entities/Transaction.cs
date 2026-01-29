using System;

namespace Portfolio.Domain.Entities;

public class Transaction
{
    public int Id { get; private set; }
    public DateTime Date { get; private set; }
    public string TransactionTypeCode { get; private set; } = null!;
    public string FromAssetId { get; private set; } = null!;
    public string ToAssetId { get; private set; } = null!;
    public decimal AmountSpent { get; private set; }
    public decimal AmountReceived { get; private set; }
    public decimal FromAssetPriceInUsd { get; private set; }
    public decimal? FromAssetPriceInEur { get; private set; }
    public decimal Fee { get; private set; }
    public string? FeeAsset { get; private set; }
    public decimal? FeeAssetPriceInUsd { get; private set; }
    public decimal? FeeAssetPriceInEur { get; private set; }
    public decimal? UsdEurExchangeRate { get; private set; }
    public string? Notes { get; private set; }

    // Navigation property (no virtual)
    public TransactionType TransactionType { get; private set; } = null!;

    // Constructor for EF Core
    private Transaction() { }

    public Transaction(
        DateTime date,
        string transactionTypeCode,
        string fromAssetId,
        string toAssetId,
        decimal amountSpent,
        decimal amountReceived,
        decimal fromAssetPriceInUsd,
        decimal? fromAssetPriceInEur,
        decimal fee,
        string? feeAsset,
        decimal? feeAssetPriceInUsd,
        decimal? feeAssetPriceInEur,
        decimal? usdEurExchangeRate,
        string? notes)
    {
        Date = date;
        TransactionTypeCode = transactionTypeCode ?? throw new ArgumentNullException(nameof(transactionTypeCode));
        FromAssetId = fromAssetId ?? throw new ArgumentNullException(nameof(fromAssetId));
        ToAssetId = toAssetId ?? throw new ArgumentNullException(nameof(toAssetId));
        AmountSpent = amountSpent;
        AmountReceived = amountReceived;
        FromAssetPriceInUsd = fromAssetPriceInUsd;
        FromAssetPriceInEur = fromAssetPriceInEur;
        Fee = fee;
        FeeAsset = feeAsset;
        FeeAssetPriceInUsd = feeAssetPriceInUsd;
        FeeAssetPriceInEur = feeAssetPriceInEur;
        UsdEurExchangeRate = usdEurExchangeRate;
        Notes = notes;
    }
    
    // Method to set navigation property if needed manually, though usually EF handles this.
    public void SetTransactionType(TransactionType type)
    {
        TransactionType = type ?? throw new ArgumentNullException(nameof(type));
        TransactionTypeCode = type.Code;
    }

    // Method to update exchange rates
    public void UpdateExchangeRates(decimal usdEurRate)
    {
        if (!FromAssetPriceInEur.HasValue)
        {
            FromAssetPriceInEur = FromAssetPriceInUsd * usdEurRate;
        }

        if (!FeeAssetPriceInEur.HasValue && FeeAssetPriceInUsd.HasValue)
        {
            FeeAssetPriceInEur = FeeAssetPriceInUsd.Value * usdEurRate;
        }

        UsdEurExchangeRate = usdEurRate;
    }
}
