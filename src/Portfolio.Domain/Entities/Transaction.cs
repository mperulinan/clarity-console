using Portfolio.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Portfolio.Domain.Entities;

public class Transaction
{
    public int Id { get; private set; }
    public DateTime Date { get; private set; }
    public string TransactionTypeCode { get; private set; } = null!;

    // Helper property for SmartEnum
    [NotMapped]
    public TransactionTypeEnum Type
    { 
        get => TransactionTypeEnum.FromValue(TransactionTypeCode); 
        private set => TransactionTypeCode = value.Value; 
    }

    public TransactionType TransactionType { get; private set; } = null!;
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

    // Constructor for EF Core.
    private Transaction() { }

    public Transaction(
        DateTime date,
        TransactionTypeEnum transactionTypeCode,
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
        TransactionTypeCode = transactionTypeCode?.Value ?? throw new ArgumentNullException(nameof(transactionTypeCode));
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

    public decimal? GetFromAssetPrice(FiatCurrency currency)
    {
        if (currency == FiatCurrency.USD) return FromAssetPriceInUsd;
        if (currency == FiatCurrency.EUR) return FromAssetPriceInEur;
        throw new ArgumentException($"Unsupported currency: {currency.Name}");
    }

    public decimal? GetToAssetPrice(FiatCurrency currency)
    {
        decimal? fromPrice = GetFromAssetPrice(currency);
        if (!fromPrice.HasValue) return null;

        if (ToAssetId == FromAssetId) return fromPrice;
        if (AmountReceived == 0) return null;

        return AmountSpent * fromPrice / AmountReceived;
    }

    public decimal? GetFeeAssetPrice(FiatCurrency currency)
    {
        if (currency == FiatCurrency.USD) return FeeAssetPriceInUsd;
        if (currency == FiatCurrency.EUR) return FeeAssetPriceInEur;
        throw new ArgumentException($"Unsupported currency: {currency.Name}");
    }
}
