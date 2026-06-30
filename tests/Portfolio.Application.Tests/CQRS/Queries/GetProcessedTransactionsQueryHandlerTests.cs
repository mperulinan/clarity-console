using NSubstitute;
using Portfolio.Application.CQRS.Queries;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.Services;

namespace Portfolio.Application.Tests.CQRS.Queries;

public class GetProcessedTransactionsQueryHandlerTests
{
    private readonly InventoryCalculator _inventoryCalculator;

    public GetProcessedTransactionsQueryHandlerTests()
    {
        _inventoryCalculator = new InventoryCalculator();
    }

    [Fact]
    public async Task Handle_WithNoCurrency_UsesTaxCurrency_ForProfitLoss()
    {
        // Arrange
        var mockUow = Substitute.For<IUnitOfWork>();
        Guid eurId = FiatCurrency.EUR.Id;
        Guid btcId = Guid.NewGuid();

        // Buy 1 BTC with 10000 EUR (Cost basis: 10000 EUR)
        // Spot price is handled through AmountSpent and AmountReceived for Swaps, but for PL calculation of crypto -> fiat, the exit price is SpotPriceEUR.
        var tx1 = new Transaction(DateTime.UtcNow.AddDays(-10), TransactionType.Swap, eurId, btcId, 10000, 1, 12000, 10000, 0, null, null, null, null, FiatCurrency.EUR, null, null)
        {
            FromAsset = new Asset("EUR", "Euro", null, null, AssetType.Fiat),
            ToAsset = new Asset("BTC", "Bitcoin", null, null, AssetType.Crypto)
        };

        // Sell 1 BTC for 15000 EUR (Proceeds: 15000 EUR)
        var tx2 = new Transaction(DateTime.UtcNow, TransactionType.Swap, btcId, eurId, 1, 15000, 20000, 15000, 0, null, null, null, null, FiatCurrency.EUR, null, null)
        {
            FromAsset = new Asset("BTC", "Bitcoin", null, null, AssetType.Crypto),
            ToAsset = new Asset("EUR", "Euro", null, null, AssetType.Fiat)
        };

        mockUow.Transactions.GetAllAsync().Returns(Task.FromResult((IEnumerable<Transaction>)[tx1, tx2]));

        var handler = new GetProcessedTransactionsQueryHandler(mockUow, _inventoryCalculator);

        // Act
        var result = await handler.Handle(new GetProcessedTransactionsQuery(), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        
        var sellTx = result.Last();
        // In EUR: Proceeds (15000) - CostBasis (10000) = 5000
        Assert.Equal(5000m, sellTx.ProfitLoss);
    }

    [Fact]
    public async Task Handle_WithSpecificCurrency_UsesProvidedCurrency_ForProfitLoss()
    {
        // Arrange
        var mockUow = Substitute.For<IUnitOfWork>();
        Guid usdId = FiatCurrency.USD.Id;
        Guid btcId = Guid.NewGuid();

        // Buy 1 BTC (Cost basis: 12000 USD, 10000 EUR)
        var tx1 = new Transaction(DateTime.UtcNow.AddDays(-10), TransactionType.Swap, usdId, btcId, 12000, 1, 12000, 10000, 0, null, null, null, null, FiatCurrency.USD, null, null)
        {
            FromAsset = new Asset("USD", "US Dollar", null, null, AssetType.Fiat),
            ToAsset = new Asset("BTC", "Bitcoin", null, null, AssetType.Crypto)
        };

        // Sell 1 BTC (Proceeds: 20000 USD, 15000 EUR)
        var tx2 = new Transaction(DateTime.UtcNow, TransactionType.Swap, btcId, usdId, 1, 20000, 20000, 15000, 0, null, null, null, null, FiatCurrency.USD, null, null)
        {
            FromAsset = new Asset("BTC", "Bitcoin", null, null, AssetType.Crypto),
            ToAsset = new Asset("USD", "US Dollar", null, null, AssetType.Fiat)
        };

        mockUow.Transactions.GetAllAsync().Returns(Task.FromResult((IEnumerable<Transaction>)[tx1, tx2]));

        var handler = new GetProcessedTransactionsQueryHandler(mockUow, _inventoryCalculator);

        // Act
        var result = await handler.Handle(new GetProcessedTransactionsQuery("USD"), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        
        var sellTx = result.Last();
        // In USD: Proceeds (20000) - CostBasis (12000) = 8000
        Assert.Equal(8000m, sellTx.ProfitLoss);
    }
}
