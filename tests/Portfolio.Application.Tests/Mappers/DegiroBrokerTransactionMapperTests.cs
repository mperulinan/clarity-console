using Portfolio.Application.Mappers;
using Portfolio.Application.Models;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;

namespace Portfolio.Application.Tests.Mappers;

public class DegiroBrokerTransactionMapperTests
{
    private readonly DegiroBrokerTransactionMapper _sut = new();

    [Fact]
    public void BrokerKey_ShouldBeDegiro()
    {
        Assert.Equal("degiro", _sut.BrokerKey);
    }

    [Fact]
    public void DisplayName_ShouldBeDegiro()
    {
        Assert.Equal("DEGIRO", _sut.DisplayName);
    }

    [Fact]
    public void MapToTransaction_BuyOrder_MapsEurToStockSwapWithPositiveAmounts()
    {
        // Arrange
        var targetAssetId = Guid.NewGuid();
        var row = new ImportedTransactionRow(
            Date: new DateTime(2026, 3, 15, 10, 30, 0, DateTimeKind.Utc),
            ProductName: "Apple Inc",
            Isin: "US0378331005",
            Exchange: "NSY",
            Quantity: 10m,
            PricePerUnit: 175m,
            PriceCurrency: "USD",
            FxRate: 1.08m,
            FeeEur: 1.5m,
            TotalAmount: -1620.37m,
            TotalCurrency: "EUR",
            BrokerTransactionId: "tx-buy-1"
        );

        // Act
        var transaction = _sut.MapToTransaction(row, targetAssetId);

        // Assert
        Assert.NotNull(transaction);
        Assert.Equal(TransactionType.Swap, transaction.Type);
        Assert.Equal(FiatCurrency.EUR.Id, transaction.FromAssetId);
        Assert.Equal(targetAssetId, transaction.ToAssetId);
        Assert.Equal(1620.37m, transaction.AmountSpent);
        Assert.Equal(10m, transaction.AmountReceived);
        Assert.Equal(1.5m, transaction.Fee);
        Assert.Equal(FiatCurrency.EUR.Id, transaction.FeeAssetId);
        Assert.Equal("Imported from DEGIRO (Apple Inc). Order ID: tx-buy-1", transaction.Notes);
        Assert.Equal("tx-buy-1", transaction.BrokerTransactionId);
        Assert.Equal(row.Date, transaction.Date);
        Assert.Equal(1m / 1.08m, transaction.UsdEurExchangeRate);
        Assert.Equal(1.08m, transaction.SpotPrice.USD);
        Assert.False(transaction.HasIncompleteExchangeRates);
    }

    [Fact]
    public void MapToTransaction_SellOrder_MapsStockToEurSwapWithPositiveAmounts()
    {
        // Arrange
        var targetAssetId = Guid.NewGuid();
        var row = new ImportedTransactionRow(
            Date: new DateTime(2026, 4, 10, 14, 0, 0, DateTimeKind.Utc),
            ProductName: "Microsoft Corp",
            Isin: "US5949181045",
            Exchange: "NDQ",
            Quantity: -5m,
            PricePerUnit: 420m,
            PriceCurrency: "USD",
            FxRate: 1.07m,
            FeeEur: 2.0m,
            TotalAmount: 1960.50m,
            TotalCurrency: "EUR",
            BrokerTransactionId: "tx-sell-1"
        );

        // Act
        var transaction = _sut.MapToTransaction(row, targetAssetId);

        // Assert
        Assert.NotNull(transaction);
        Assert.Equal(TransactionType.Swap, transaction.Type);
        Assert.Equal(targetAssetId, transaction.FromAssetId);
        Assert.Equal(FiatCurrency.EUR.Id, transaction.ToAssetId);
        Assert.Equal(5m, transaction.AmountSpent);
        Assert.Equal(1960.50m, transaction.AmountReceived);
        Assert.Equal(2.0m, transaction.Fee);
        Assert.Equal(FiatCurrency.EUR.Id, transaction.FeeAssetId);
        Assert.Equal("Imported from DEGIRO (Microsoft Corp). Order ID: tx-sell-1", transaction.Notes);
        Assert.Equal("tx-sell-1", transaction.BrokerTransactionId);
        Assert.Equal(1m / 1.07m, transaction.UsdEurExchangeRate);
        Assert.False(transaction.HasIncompleteExchangeRates);
    }

    [Fact]
    public void MapToTransaction_EurTradeWithoutFxRate_LeavesExchangeRatesIncompleteForBackgroundSync()
    {
        // Arrange
        var targetAssetId = Guid.NewGuid();
        var row = new ImportedTransactionRow(
            Date: new DateTime(2026, 4, 10, 14, 0, 0, DateTimeKind.Utc),
            ProductName: "SAP SE",
            Isin: "DE0007164600",
            Exchange: "XET",
            Quantity: 5m,
            PricePerUnit: 150m,
            PriceCurrency: "EUR",
            FxRate: null,
            FeeEur: 1.0m,
            TotalAmount: -751m,
            TotalCurrency: "EUR",
            BrokerTransactionId: "tx-eur-1"
        );

        // Act
        var transaction = _sut.MapToTransaction(row, targetAssetId);

        // Assert
        Assert.Null(transaction.UsdEurExchangeRate);
        Assert.Null(transaction.SpotPrice.USD);
        Assert.True(transaction.HasIncompleteExchangeRates);
    }

    [Fact]
    public void MapToTransaction_NullFee_DefaultsFeeToZeroEur()
    {
        // Arrange
        var targetAssetId = Guid.NewGuid();
        var row = new ImportedTransactionRow(
            Date: DateTime.UtcNow,
            ProductName: "Apple Inc",
            Isin: "US0378331005",
            Exchange: "NSY",
            Quantity: 2m,
            PricePerUnit: 180m,
            PriceCurrency: "USD",
            FxRate: null,
            FeeEur: null,
            TotalAmount: -330m,
            TotalCurrency: "EUR",
            BrokerTransactionId: "tx-no-fee"
        );

        // Act
        var transaction = _sut.MapToTransaction(row, targetAssetId);

        // Assert
        Assert.Equal(0m, transaction.Fee);
        Assert.Equal(FiatCurrency.EUR.Id, transaction.FeeAssetId);
    }

    [Fact]
    public void MapToTransaction_DepositRow_MapsToDepositTransactionInEur()
    {
        // Arrange
        var row = new ImportedTransactionRow(
            Date: new DateTime(2026, 1, 10, 9, 0, 0, DateTimeKind.Utc),
            ProductName: "flatex Deposit",
            Isin: string.Empty,
            Exchange: string.Empty,
            Quantity: 2500m,
            PricePerUnit: 1m,
            PriceCurrency: "EUR",
            FxRate: null,
            FeeEur: null,
            TotalAmount: 2500m,
            TotalCurrency: "EUR",
            BrokerTransactionId: "dep-1",
            TransactionType: TransactionType.Deposit
        );

        // Act
        var transaction = _sut.MapToTransaction(row, FiatCurrency.EUR.Id);

        // Assert
        Assert.Equal(TransactionType.Deposit, transaction.Type);
        Assert.Null(transaction.FromAssetId);
        Assert.Equal(FiatCurrency.EUR.Id, transaction.ToAssetId);
        Assert.Equal(0m, transaction.AmountSpent);
        Assert.Equal(2500m, transaction.AmountReceived);
        Assert.Equal("dep-1", transaction.BrokerTransactionId);
    }

    [Fact]
    public void MapToTransaction_WithdrawalRow_MapsToWithdrawalTransactionInEur()
    {
        // Arrange
        var row = new ImportedTransactionRow(
            Date: new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc),
            ProductName: "flatex Withdrawal",
            Isin: string.Empty,
            Exchange: string.Empty,
            Quantity: 1000m,
            PricePerUnit: 1m,
            PriceCurrency: "EUR",
            FxRate: null,
            FeeEur: null,
            TotalAmount: -1000m,
            TotalCurrency: "EUR",
            BrokerTransactionId: "wth-1",
            TransactionType: TransactionType.Withdrawal
        );

        // Act
        var transaction = _sut.MapToTransaction(row, FiatCurrency.EUR.Id);

        // Assert
        Assert.Equal(TransactionType.Withdrawal, transaction.Type);
        Assert.Equal(FiatCurrency.EUR.Id, transaction.FromAssetId);
        Assert.Null(transaction.ToAssetId);
        Assert.Equal(1000m, transaction.AmountSpent);
        Assert.Equal(0m, transaction.AmountReceived);
        Assert.Equal("wth-1", transaction.BrokerTransactionId);
    }

    [Fact]
    public void MapToTransaction_RewardRow_MapsToRewardTransaction()
    {
        // Arrange
        var row = new ImportedTransactionRow(
            Date: new DateTime(2026, 2, 1, 8, 0, 0, DateTimeKind.Utc),
            ProductName: "Dividendo",
            Isin: "US01609W1027",
            Exchange: string.Empty,
            Quantity: 15m,
            PricePerUnit: 1m,
            PriceCurrency: "EUR",
            FxRate: null,
            FeeEur: null,
            TotalAmount: 15m,
            TotalCurrency: "EUR",
            BrokerTransactionId: "div-1",
            TransactionType: TransactionType.Reward
        );

        // Act
        var transaction = _sut.MapToTransaction(row, FiatCurrency.EUR.Id);

        // Assert
        Assert.Equal(TransactionType.Reward, transaction.Type);
        Assert.Null(transaction.FromAssetId);
        Assert.Equal(FiatCurrency.EUR.Id, transaction.ToAssetId);
        Assert.Equal(0m, transaction.AmountSpent);
        Assert.Equal(15m, transaction.AmountReceived);
    }

    [Fact]
    public void MapToTransaction_LossRow_MapsToLossTransaction()
    {
        // Arrange
        var row = new ImportedTransactionRow(
            Date: new DateTime(2026, 2, 5, 10, 0, 0, DateTimeKind.Utc),
            ProductName: "Comisión de conectividad con el mercado",
            Isin: string.Empty,
            Exchange: string.Empty,
            Quantity: 2.5m,
            PricePerUnit: 1m,
            PriceCurrency: "EUR",
            FxRate: null,
            FeeEur: null,
            TotalAmount: -2.5m,
            TotalCurrency: "EUR",
            BrokerTransactionId: "fee-conn-1",
            TransactionType: TransactionType.Loss
        );

        // Act
        var transaction = _sut.MapToTransaction(row, FiatCurrency.EUR.Id);

        // Assert
        Assert.Equal(TransactionType.Loss, transaction.Type);
        Assert.Equal(FiatCurrency.EUR.Id, transaction.FromAssetId);
        Assert.Null(transaction.ToAssetId);
        Assert.Equal(2.5m, transaction.AmountSpent);
        Assert.Equal(0m, transaction.AmountReceived);
    }

    [Fact]
    public void MapToTransaction_UsdRewardRowWithoutFxRate_MapsWithUsdInputCurrencyAndIncompleteExchangeRate()
    {
        // Arrange
        var row = new ImportedTransactionRow(
            Date: new DateTime(2026, 2, 1, 8, 0, 0, DateTimeKind.Utc),
            ProductName: "Dividendo USD",
            Isin: "US01609W1027",
            Exchange: string.Empty,
            Quantity: 25m,
            PricePerUnit: 1m,
            PriceCurrency: "USD",
            FxRate: null,
            FeeEur: null,
            TotalAmount: 25m,
            TotalCurrency: "USD",
            BrokerTransactionId: "div-usd-1",
            TransactionType: TransactionType.Reward
        );

        // Act
        var transaction = _sut.MapToTransaction(row, FiatCurrency.USD.Id);

        // Assert
        Assert.Equal(TransactionType.Reward, transaction.Type);
        Assert.Equal(FiatCurrency.USD, transaction.SpotPrice.InputCurrency);
        Assert.Equal(1m, transaction.SpotPrice.USD);
        Assert.Null(transaction.SpotPrice.EUR);
        Assert.True(transaction.HasIncompleteExchangeRates);
    }

    [Fact]
    public void MapToTransaction_UsdRewardRowWithFxRate_UpdatesExchangeRates()
    {
        // Arrange
        var row = new ImportedTransactionRow(
            Date: new DateTime(2026, 2, 1, 8, 0, 0, DateTimeKind.Utc),
            ProductName: "Dividendo USD",
            Isin: "US01609W1027",
            Exchange: string.Empty,
            Quantity: 25m,
            PricePerUnit: 1m,
            PriceCurrency: "USD",
            FxRate: 1.10m, // DEGIRO EUR -> USD
            FeeEur: null,
            TotalAmount: 25m,
            TotalCurrency: "USD",
            BrokerTransactionId: "div-usd-2",
            TransactionType: TransactionType.Reward
        );

        // Act
        var transaction = _sut.MapToTransaction(row, FiatCurrency.USD.Id);

        // Assert
        Assert.Equal(TransactionType.Reward, transaction.Type);
        Assert.Equal(FiatCurrency.USD, transaction.SpotPrice.InputCurrency);
        Assert.Equal(1m, transaction.SpotPrice.USD);
        Assert.NotNull(transaction.SpotPrice.EUR);
        Assert.Equal(1m / 1.10m, transaction.SpotPrice.EUR.Value);
        Assert.Equal(1m / 1.10m, transaction.UsdEurExchangeRate);
        Assert.False(transaction.HasIncompleteExchangeRates);
    }
}
