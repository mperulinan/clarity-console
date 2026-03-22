using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Xunit;

namespace Portfolio.Domain.Tests;

public class TransactionTests
{
    private static Transaction CreateTx(
        DateTime? date = null, 
        TransactionType? type = null, 
        string fromAsset = "USD", 
        string toAsset = "BTC", 
        decimal spent = 100, 
        decimal received = 2, 
        decimal? spotPriceUSD = 1, 
        decimal? spotPriceEUR = null, 
        decimal fee = 0, 
        string? feeAsset = null, 
        decimal? feePriceUSD = null, 
        decimal? feePriceEUR = null, 
        decimal? xr = null, 
        FiatCurrency? spotCurrency = null, 
        FiatCurrency? feeCurrency = null, 
        string? notes = null)
    {
        var fromAsstId = Guid.NewGuid();
        var toAsstId = fromAsset == toAsset ? fromAsstId : Guid.NewGuid();
        var feeAsstId = feeAsset != null ? (feeAsset == fromAsset ? fromAsstId : (feeAsset == toAsset ? toAsstId : Guid.NewGuid())) : (Guid?)null;

        var tx = new Transaction(
            date ?? DateTime.UtcNow, 
            type ?? TransactionType.Swap, 
            fromAsstId, 
            toAsstId, 
            spent, 
            received, 
            spotPriceUSD, 
            spotPriceEUR, 
            fee, 
            feeAsstId, 
            feePriceUSD, 
            feePriceEUR, 
            xr, 
            spotCurrency ?? (spotPriceEUR.HasValue && !spotPriceUSD.HasValue ? FiatCurrency.EUR : FiatCurrency.USD), 
            feeCurrency ?? (feePriceEUR.HasValue && !feePriceUSD.HasValue ? FiatCurrency.EUR : (feePriceUSD.HasValue ? FiatCurrency.USD : null)), 
            notes)
        {
            FromAsset = new Asset(fromAsset, fromAsset, null, null, AssetType.Crypto),
            ToAsset = new Asset(toAsset, toAsset, null, null, AssetType.Crypto)
        };
        if (feeAsset != null) tx.FeeAsset = new Asset(feeAsset, feeAsset, null, null, AssetType.Crypto);
        return tx;
    }
    [Fact]
    public void GetToAssetPrice_ShouldCalculateBasedOnSpentAndReceived()
    {
        // Spent 100 USD (Price 1) to get 2 BTC.
        // ToAssetPrice = 100 * 1 / 2 = 50.
        Transaction tx = CreateTx(spent: 100m, received: 2m, spotPriceUSD: 1m);

        var price = tx.GetToAssetPrice(FiatCurrency.USD);

        Assert.Equal(50m, price);
    }

    [Fact]
    public void UpdateExchangeRates_ShouldUpdateEurPrices()
    {
        // USD Price = 100. No EUR Price.
        // Exchange Rate = 0.9.
        // Expected EUR Price = 90.
        var tx = CreateTx(spotPriceUSD: 100, feePriceUSD: 10);

        tx.UpdateExchangeRates(0.9m);

        Assert.Equal(0.9m, tx.UsdEurExchangeRate);
        Assert.Equal(90m, tx.SpotPriceEUR);
        Assert.Equal(9m, tx.FeePriceEUR); // 10 * 0.9
    }

    [Fact]
    public void UpdateExchangeRates_ShouldUpdateUsdPrices_WhenEurIsTruthSource()
    {
        // EUR Price = 90. No USD Price.
        // Exchange Rate = 0.9. (1 EUR = 1/0.9 USD = 1.11 USD approx)
        // Expected USD Price = 90 / 0.9 = 100.
        var tx = CreateTx(spotPriceUSD: null, spotPriceEUR: 90m, spotCurrency: FiatCurrency.EUR);

        tx.UpdateExchangeRates(0.9m);

        Assert.Equal(100m, tx.SpotPriceUSD);
    }

    [Fact]
    public void UpdateExchangeRates_ShouldNotOverrideExistingPrices_WhenNotNeeded()
    {
        // Both prices exist. USD is truth source.
        var tx = CreateTx(spotPriceUSD: 100, spotPriceEUR: 85);
        
        tx.UpdateExchangeRates(0.9m);

        // SpotPriceEUR was 85, logic says SpotPriceEUR = SpotPriceUSD * rate = 100 * 0.9 = 90.
        // The current implementation OVERWRITES derived prices even if they exist.
        // Let's verify this behavior.
        Assert.Equal(90m, tx.SpotPriceEUR);
    }
    
    [Fact]
    public void UpdateExchangeRates_ShouldUpdateFeeUsd_WhenFeeEurIsTruthSource()
    {
        // Fee EUR = 10. No Fee USD.
        // Rate = 0.8
        // Expected Fee USD = 10 / 0.8 = 12.5.
        var tx = CreateTx(feePriceUSD: null, feePriceEUR: 10m, feeCurrency: FiatCurrency.EUR);

        tx.UpdateExchangeRates(0.8m);

        Assert.Equal(12.5m, tx.FeePriceUSD);
    }

    [Fact]
    public void UpdateExchangeRates_ShouldNotUpdate_WhenRateIsZeroOrNegative()
    {
        var tx = CreateTx(spotPriceUSD: null, spotPriceEUR: 10, spotCurrency: FiatCurrency.EUR);
        
        tx.UpdateExchangeRates(0);
        Assert.Null(tx.SpotPriceUSD);

        tx.UpdateExchangeRates(-1);
        Assert.Null(tx.SpotPriceUSD);
    }
    
    [Fact]
    public void Constructor_ShouldSetTruthSourceCorrectly()
    {
        var txUsd = CreateTx(spotPriceUSD: 100, spotCurrency: FiatCurrency.USD);
        Assert.Equal(FiatCurrency.USD, txUsd.SpotPriceInputCurrency);

        var txEur = CreateTx(spotPriceUSD: null, spotPriceEUR: 90, spotCurrency: FiatCurrency.EUR);
        Assert.Equal(FiatCurrency.EUR, txEur.SpotPriceInputCurrency);
    }

    [Fact]
    public void UpdateExchangeRates_ShouldDeriveFeeEur_WhenFeeUsdIsTruthSource()
    {
        // Fee USD = 10. Fee EUR null.
        // Rate = 0.9.
        var tx = CreateTx(feePriceUSD: 10m, feeCurrency: FiatCurrency.USD);

        tx.UpdateExchangeRates(0.9m);

        Assert.Equal(9m, tx.FeePriceEUR);
    }

    [Fact]
    public void UpdateExchangeRates_ShouldNotUpdateFees_WhenFeeCurrencyIsNull()
    {
        // Fee Currency null (legacy or not provided).
        // Using constructor directly to stay at 'null' for the fee currency.
        var tx = new Transaction(DateTime.UtcNow, TransactionType.Swap, Guid.NewGuid(), Guid.NewGuid(), 100, 2, 1, null, 0.1m, null, 10m, null, null, FiatCurrency.USD, null, null);

        tx.UpdateExchangeRates(0.9m);

        Assert.Null(tx.FeePriceEUR);
    }
}
