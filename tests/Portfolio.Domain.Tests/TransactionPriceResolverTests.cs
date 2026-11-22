using Portfolio.Domain.Enums;
using Portfolio.Domain.Services;
using Xunit;

namespace Portfolio.Domain.Tests;

public class TransactionPriceResolverTests
{
    [Fact]
    public void ResolveSpotPrice_PricedAssetIsFiatEur_SetsSpotPriceEurToOne()
    {
        // Arrange: Deposit EUR (ToAsset = EUR, FromAsset = null)
        var toAssetId = FiatCurrency.EUR.Id;

        // Act
        var result = TransactionPriceResolver.ResolveSpotPrice(
            TransactionType.Deposit,
            fromAssetId: null,
            toAssetId: toAssetId,
            amountSpent: 0m,
            amountReceived: 1000m,
            spotPriceUSD: null,
            spotPriceEUR: null,
            spotPriceInputCurrency: null);

        // Assert
        Assert.Equal(1m, result.EUR);
        Assert.Null(result.USD);
        Assert.Equal(FiatCurrency.EUR, result.InputCurrency);
    }

    [Fact]
    public void ResolveSpotPrice_PricedAssetIsFiatUsd_SetsSpotPriceUsdToOne()
    {
        // Arrange: Reward in USD (ToAsset = USD, FromAsset = null)
        var toAssetId = FiatCurrency.USD.Id;

        // Act
        var result = TransactionPriceResolver.ResolveSpotPrice(
            TransactionType.Reward,
            fromAssetId: null,
            toAssetId: toAssetId,
            amountSpent: 0m,
            amountReceived: 50m,
            spotPriceUSD: null,
            spotPriceEUR: null,
            spotPriceInputCurrency: null);

        // Assert
        Assert.Equal(1m, result.USD);
        Assert.Null(result.EUR);
        Assert.Equal(FiatCurrency.USD, result.InputCurrency);
    }

    [Fact]
    public void ResolveSpotPrice_SellingAssetForUsdFiat_DerivesSpotPriceUsdFromAmounts()
    {
        // Arrange: Selling 2 AAPL for 300 USD (FromAsset = AAPL, ToAsset = USD)
        var stockId = Guid.NewGuid();
        var usdId = FiatCurrency.USD.Id;

        // Act
        var result = TransactionPriceResolver.ResolveSpotPrice(
            TransactionType.Swap,
            fromAssetId: stockId,
            toAssetId: usdId,
            amountSpent: 2m,
            amountReceived: 300m,
            spotPriceUSD: null,
            spotPriceEUR: null,
            spotPriceInputCurrency: null);

        // Assert: 300 USD / 2 units = 150 USD per unit
        Assert.Equal(150m, result.USD);
        Assert.Null(result.EUR);
        Assert.Equal(FiatCurrency.USD, result.InputCurrency);
    }

    [Fact]
    public void ResolveSpotPrice_SellingAssetForEurFiat_DerivesSpotPriceEurFromAmounts()
    {
        // Arrange: Selling 5 Stock for 500 EUR (FromAsset = Stock, ToAsset = EUR)
        var stockId = Guid.NewGuid();
        var eurId = FiatCurrency.EUR.Id;

        // Act
        var result = TransactionPriceResolver.ResolveSpotPrice(
            TransactionType.Swap,
            fromAssetId: stockId,
            toAssetId: eurId,
            amountSpent: 5m,
            amountReceived: 500m,
            spotPriceUSD: null,
            spotPriceEUR: null,
            spotPriceInputCurrency: null);

        // Assert: 500 EUR / 5 units = 100 EUR per unit
        Assert.Equal(100m, result.EUR);
        Assert.Null(result.USD);
        Assert.Equal(FiatCurrency.EUR, result.InputCurrency);
    }

    [Fact]
    public void ResolveSpotPrice_NonFiatSwapWithExplicitPrices_PreservesInputs()
    {
        // Arrange: Crypto-to-crypto swap with explicit USD spot price
        var btcId = Guid.NewGuid();
        var ethId = Guid.NewGuid();

        // Act
        var result = TransactionPriceResolver.ResolveSpotPrice(
            TransactionType.Swap,
            fromAssetId: btcId,
            toAssetId: ethId,
            amountSpent: 1m,
            amountReceived: 15m,
            spotPriceUSD: 60000m,
            spotPriceEUR: 55000m,
            spotPriceInputCurrency: FiatCurrency.USD);

        // Assert
        Assert.Equal(60000m, result.USD);
        Assert.Equal(55000m, result.EUR);
        Assert.Equal(FiatCurrency.USD, result.InputCurrency);
    }

    [Fact]
    public void ResolveSpotPrice_MissingInputCurrency_ThrowsArgumentException()
    {
        // Arrange
        var asset1 = Guid.NewGuid();
        var asset2 = Guid.NewGuid();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            TransactionPriceResolver.ResolveSpotPrice(
                TransactionType.Swap,
                fromAssetId: asset1,
                toAssetId: asset2,
                amountSpent: 1m,
                amountReceived: 1m,
                spotPriceUSD: 100m,
                spotPriceEUR: null,
                spotPriceInputCurrency: null));
    }

    [Fact]
    public void ResolveSpotPrice_MissingBothPrices_ThrowsArgumentException()
    {
        // Arrange
        var asset1 = Guid.NewGuid();
        var asset2 = Guid.NewGuid();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            TransactionPriceResolver.ResolveSpotPrice(
                TransactionType.Swap,
                fromAssetId: asset1,
                toAssetId: asset2,
                amountSpent: 1m,
                amountReceived: 1m,
                spotPriceUSD: null,
                spotPriceEUR: null,
                spotPriceInputCurrency: FiatCurrency.USD));
    }

    [Fact]
    public void ResolveFeePrice_FeeAssetIsEur_SetsPriceEurToOneAndInputCurrencyEur()
    {
        // Act
        var result = TransactionPriceResolver.ResolveFeePrice(
            feeAssetId: FiatCurrency.EUR.Id,
            feeSpotPriceUSD: null,
            feeSpotPriceEUR: null,
            feePriceInputCurrency: null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1m, result.EUR);
        Assert.Null(result.USD);
        Assert.Equal(FiatCurrency.EUR, result.InputCurrency);
    }

    [Fact]
    public void ResolveFeePrice_FeeAssetIsUsd_SetsPriceUsdToOneAndInputCurrencyUsd()
    {
        // Act
        var result = TransactionPriceResolver.ResolveFeePrice(
            feeAssetId: FiatCurrency.USD.Id,
            feeSpotPriceUSD: null,
            feeSpotPriceEUR: null,
            feePriceInputCurrency: null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1m, result.USD);
        Assert.Null(result.EUR);
        Assert.Equal(FiatCurrency.USD, result.InputCurrency);
    }

    [Fact]
    public void ResolveFeePrice_NonFiatFee_PreservesCallerInputs()
    {
        // Arrange
        var cryptoFeeId = Guid.NewGuid();

        // Act
        var result = TransactionPriceResolver.ResolveFeePrice(
            feeAssetId: cryptoFeeId,
            feeSpotPriceUSD: 2500m,
            feeSpotPriceEUR: 2300m,
            feePriceInputCurrency: FiatCurrency.USD);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2500m, result.USD);
        Assert.Equal(2300m, result.EUR);
        Assert.Equal(FiatCurrency.USD, result.InputCurrency);
    }

    [Fact]
    public void ResolveFeePrice_NoFee_ReturnsNull()
    {
        // Act
        var result = TransactionPriceResolver.ResolveFeePrice(
            feeAssetId: null,
            feeSpotPriceUSD: null,
            feeSpotPriceEUR: null,
            feePriceInputCurrency: null);

        // Assert
        Assert.Null(result);
    }
}
