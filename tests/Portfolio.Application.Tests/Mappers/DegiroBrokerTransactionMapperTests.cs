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
            TotalEur: -1620.37m,
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
            TotalEur: 1960.50m,
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
            TotalEur: -330m,
            BrokerTransactionId: "tx-no-fee"
        );

        // Act
        var transaction = _sut.MapToTransaction(row, targetAssetId);

        // Assert
        Assert.Equal(0m, transaction.Fee);
        Assert.Equal(FiatCurrency.EUR.Id, transaction.FeeAssetId);
    }
}
