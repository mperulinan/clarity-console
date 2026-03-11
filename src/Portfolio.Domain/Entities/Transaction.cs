using Portfolio.Domain.Enums;
using Portfolio.Domain.ValueObjects;
using System;

namespace Portfolio.Domain.Entities;

public class Transaction
{
    public int Id { get; private set; }
    public DateTime Date { get; private set; }
    public TransactionType Type { get; private set; } = null!;
    public Guid? FromAssetId { get; private set; }
    public Asset? FromAsset { get; set; }
    public Guid? ToAssetId { get; private set; }
    public Asset? ToAsset { get; set; }
    public decimal AmountSpent { get; private set; }
    public decimal AmountReceived { get; private set; }
    public decimal SpotPriceInUsd { get; private set; }
    public decimal? SpotPriceInEur { get; private set; }
    public decimal Fee { get; private set; }
    public Guid? FeeAssetId { get; private set; }
    public Asset? FeeAsset { get; set; }
    public decimal? FeeSpotPriceInUsd { get; private set; }
    public decimal? FeeSpotPriceInEur { get; private set; }
    public decimal? UsdEurExchangeRate { get; private set; }
    public string? Notes { get; private set; }

    // Constructor for EF Core.
    private Transaction() { }

    public Transaction(
        DateTime date,
        TransactionType transactionType,
        Guid? fromAssetId,
        Guid? toAssetId,
        decimal amountSpent,
        decimal amountReceived,
        decimal spotPriceInUsd,
        decimal? spotPriceInEur,
        decimal fee,
        Guid? feeAssetId,
        decimal? feeSpotPriceInUsd,
        decimal? feeSpotPriceInEur,
        decimal? usdEurExchangeRate,
        string? notes)
    {
        Date = date;
        Type = transactionType ?? throw new ArgumentNullException(nameof(transactionType));

        if (Type.RequiresFromAsset && !fromAssetId.HasValue)
            throw new ArgumentException($"FromAssetId is required for {Type.Name}");
        if (!Type.RequiresFromAsset && fromAssetId.HasValue)
            throw new ArgumentException($"FromAssetId must be null for {Type.Name}");
            
        if (Type.RequiresToAsset && !toAssetId.HasValue)
            throw new ArgumentException($"ToAssetId is required for {Type.Name}");
        if (!Type.RequiresToAsset && toAssetId.HasValue)
            throw new ArgumentException($"ToAssetId must be null for {Type.Name}");

        FromAssetId = fromAssetId;
        ToAssetId = toAssetId;
        AmountSpent = amountSpent;
        AmountReceived = amountReceived;
        SpotPriceInUsd = spotPriceInUsd;
        SpotPriceInEur = spotPriceInEur;
        Fee = fee;
        FeeAssetId = feeAssetId;
        FeeSpotPriceInUsd = feeSpotPriceInUsd;
        FeeSpotPriceInEur = feeSpotPriceInEur;
        UsdEurExchangeRate = usdEurExchangeRate;
        Notes = notes;
    }

    public void UpdateExchangeRates(decimal usdEurRate)
    {
        if (!SpotPriceInEur.HasValue)
        {
            SpotPriceInEur = SpotPriceInUsd * usdEurRate;
        }

        if (!FeeSpotPriceInEur.HasValue && FeeSpotPriceInUsd.HasValue)
        {
            FeeSpotPriceInEur = FeeSpotPriceInUsd.Value * usdEurRate;
        }

        UsdEurExchangeRate = usdEurRate;
    }

    public decimal? GetFromAssetPrice(FiatCurrency currency)
    {
        if (Type == TransactionType.Reward || Type == TransactionType.Deposit) return null;

        if (currency == FiatCurrency.USD) return SpotPriceInUsd;
        if (currency == FiatCurrency.EUR) return SpotPriceInEur;
        throw new ArgumentException($"Unsupported currency: {currency.Name}");
    }

    public decimal? GetToAssetPrice(FiatCurrency currency)
    {
        if (Type == TransactionType.Reward || Type == TransactionType.Deposit)
        {
            if (currency == FiatCurrency.USD) return SpotPriceInUsd;
            if (currency == FiatCurrency.EUR) return SpotPriceInEur;
            return null;
        }

        if (Type == TransactionType.Withdrawal) return null;

        // For Swap: derive ToAsset price from FromAsset price (SpotPrice)
        decimal? fromPrice = GetFromAssetPrice(currency);
        if (!fromPrice.HasValue || AmountReceived == 0) return null;

        return AmountSpent * fromPrice / AmountReceived;
    }

    public decimal? GetFeeAssetPrice(FiatCurrency currency)
    {
        if (currency == FiatCurrency.USD) return FeeSpotPriceInUsd;
        if (currency == FiatCurrency.EUR) return FeeSpotPriceInEur;
        throw new ArgumentException($"Unsupported currency: {currency.Name}");
    }
}
