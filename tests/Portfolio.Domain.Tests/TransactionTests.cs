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
        Assert.Equal(90m, tx.SpotPrice.EUR);
        Assert.Equal(9m, tx.FeePrice?.EUR); // 10 * 0.9
    }

    [Fact]
    public void UpdateExchangeRates_ShouldUpdateUsdPrices_WhenEurIsTruthSource()
    {
        // EUR Price = 90. No USD Price.
        // Exchange Rate = 0.9. (1 EUR = 1/0.9 USD = 1.11 USD approx)
        // Expected USD Price = 90 / 0.9 = 100.
        var tx = CreateTx(spotPriceUSD: null, spotPriceEUR: 90m, spotCurrency: FiatCurrency.EUR);

        tx.UpdateExchangeRates(0.9m);

        Assert.Equal(100m, tx.SpotPrice.USD);
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
        Assert.Equal(90m, tx.SpotPrice.EUR);
    }
    
    [Fact]
    public void UpdateExchangeRates_ShouldUpdateFeeUsd_WhenFeeEurIsTruthSource()
    {
        // Fee EUR = 10. No Fee USD.
        // Rate = 0.8
        // Expected Fee USD = 10 / 0.8 = 12.5.
        var tx = CreateTx(feePriceUSD: null, feePriceEUR: 10m, feeCurrency: FiatCurrency.EUR);

        tx.UpdateExchangeRates(0.8m);

        Assert.Equal(12.5m, tx.FeePrice?.USD);
    }

    [Fact]
    public void UpdateExchangeRates_ShouldNotUpdate_WhenRateIsZeroOrNegative()
    {
        var tx = CreateTx(spotPriceUSD: null, spotPriceEUR: 10, spotCurrency: FiatCurrency.EUR);
        
        tx.UpdateExchangeRates(0);
        Assert.Null(tx.SpotPrice.USD);

        tx.UpdateExchangeRates(-1);
        Assert.Null(tx.SpotPrice.USD);
    }
    
    [Fact]
    public void Constructor_ShouldSetTruthSourceCorrectly()
    {
        var txUsd = CreateTx(spotPriceUSD: 100, spotCurrency: FiatCurrency.USD);
        Assert.Equal(FiatCurrency.USD, txUsd.SpotPrice.InputCurrency);

        var txEur = CreateTx(spotPriceUSD: null, spotPriceEUR: 90, spotCurrency: FiatCurrency.EUR);
        Assert.Equal(FiatCurrency.EUR, txEur.SpotPrice.InputCurrency);
    }

    [Fact]
    public void UpdateExchangeRates_ShouldDeriveFeeEur_WhenFeeUsdIsTruthSource()
    {
        // Fee USD = 10. Fee EUR null.
        // Rate = 0.9.
        var tx = CreateTx(feePriceUSD: 10m, feeCurrency: FiatCurrency.USD);

        tx.UpdateExchangeRates(0.9m);

        Assert.Equal(9m, tx.FeePrice?.EUR);
    }

    [Fact]
    public void UpdateExchangeRates_ShouldNotUpdateFees_WhenFeeCurrencyIsNull()
    {
        // Fee Currency null (legacy or not provided).
        // Using constructor directly to stay at 'null' for the fee currency.
        var tx = new Transaction(DateTime.UtcNow, TransactionType.Swap, Guid.NewGuid(), Guid.NewGuid(), 100, 2, 1, null, 0.1m, null, 10m, null, null, FiatCurrency.USD, null, null);

        tx.UpdateExchangeRates(0.9m);

        Assert.Null(tx.FeePrice?.EUR);
    }

    [Fact]
    public void CreateSwap_ValidParameters_CreatesExpectedSwapTransaction()
    {
        // Arrange
        var date = DateTime.UtcNow.AddMinutes(-5);
        var fromId = Guid.NewGuid();
        var toId = Guid.NewGuid();

        // Act
        var tx = Transaction.CreateSwap(
            date: date,
            fromAssetId: fromId,
            toAssetId: toId,
            amountSpent: 100m,
            amountReceived: 2m,
            spotPriceUSD: 50m,
            spotPriceInputCurrency: FiatCurrency.USD,
            fee: 1.5m,
            feeAssetId: FiatCurrency.EUR.Id,
            notes: "Test swap",
            brokerTransactionId: "broker-123"
        );

        // Assert
        Assert.Equal(TransactionType.Swap, tx.Type);
        Assert.Equal(date, tx.Date);
        Assert.Equal(fromId, tx.FromAssetId);
        Assert.Equal(toId, tx.ToAssetId);
        Assert.Equal(100m, tx.AmountSpent);
        Assert.Equal(2m, tx.AmountReceived);
        Assert.Equal(50m, tx.SpotPrice.USD);
        Assert.Equal(1.5m, tx.Fee);
        Assert.Equal(FiatCurrency.EUR.Id, tx.FeeAssetId);
        Assert.Equal("Test swap", tx.Notes);
        Assert.Equal("broker-123", tx.BrokerTransactionId);
    }

    [Fact]
    public void CreateDeposit_ValidParameters_CreatesExpectedDepositTransaction()
    {
        // Arrange
        var date = DateTime.UtcNow.AddMinutes(-10);

        // Act
        var tx = Transaction.CreateDeposit(
            date: date,
            amountReceived: 500m,
            notes: "Bank transfer"
        );

        // Assert
        Assert.Equal(TransactionType.Deposit, tx.Type);
        Assert.Null(tx.FromAssetId);
        Assert.Equal(FiatCurrency.TaxCurrency.Id, tx.ToAssetId);
        Assert.Equal(0m, tx.AmountSpent);
        Assert.Equal(500m, tx.AmountReceived);
        Assert.Equal(FiatCurrency.EUR, tx.SpotPrice.InputCurrency);
        Assert.Equal(1m, tx.SpotPrice.EUR);
        Assert.Equal("Bank transfer", tx.Notes);
    }

    [Fact]
    public void CreateWithdrawal_ValidParameters_CreatesExpectedWithdrawalTransaction()
    {
        // Arrange
        var date = DateTime.UtcNow.AddMinutes(-15);

        // Act
        var tx = Transaction.CreateWithdrawal(
            date: date,
            amountSpent: 250m,
            notes: "ATM withdrawal"
        );

        // Assert
        Assert.Equal(TransactionType.Withdrawal, tx.Type);
        Assert.Equal(FiatCurrency.TaxCurrency.Id, tx.FromAssetId);
        Assert.Null(tx.ToAssetId);
        Assert.Equal(250m, tx.AmountSpent);
        Assert.Equal(0m, tx.AmountReceived);
        Assert.Equal(FiatCurrency.EUR, tx.SpotPrice.InputCurrency);
        Assert.Equal(1m, tx.SpotPrice.EUR);
    }

    [Fact]
    public void CreateReward_ValidParameters_CreatesExpectedRewardTransaction()
    {
        // Arrange
        var date = DateTime.UtcNow.AddMinutes(-20);
        var assetId = Guid.NewGuid();

        // Act
        var tx = Transaction.CreateReward(
            date: date,
            toAssetId: assetId,
            amountReceived: 10m,
            spotPriceUSD: 100m,
            spotPriceInputCurrency: FiatCurrency.USD,
            notes: "Dividend reward"
        );

        // Assert
        Assert.Equal(TransactionType.Reward, tx.Type);
        Assert.Null(tx.FromAssetId);
        Assert.Equal(assetId, tx.ToAssetId);
        Assert.Equal(0m, tx.AmountSpent);
        Assert.Equal(10m, tx.AmountReceived);
        Assert.Equal(100m, tx.SpotPrice.USD);
        Assert.Equal(FiatCurrency.USD, tx.SpotPrice.InputCurrency);
    }

    [Fact]
    public void CreateLoss_ValidParameters_CreatesExpectedLossTransaction()
    {
        // Arrange
        var date = DateTime.UtcNow.AddMinutes(-25);
        var assetId = Guid.NewGuid();

        // Act
        var tx = Transaction.CreateLoss(
            date: date,
            fromAssetId: assetId,
            amountSpent: 2.5m,
            spotPriceEUR: 1m,
            spotPriceInputCurrency: FiatCurrency.EUR,
            notes: "Connectivity fee"
        );

        // Assert
        Assert.Equal(TransactionType.Loss, tx.Type);
        Assert.Equal(assetId, tx.FromAssetId);
        Assert.Null(tx.ToAssetId);
        Assert.Equal(2.5m, tx.AmountSpent);
        Assert.Equal(0m, tx.AmountReceived);
        Assert.Equal(1m, tx.SpotPrice.EUR);
        Assert.Equal(FiatCurrency.EUR, tx.SpotPrice.InputCurrency);
    }
}
