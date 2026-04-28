using NSubstitute;
using Portfolio.Application.CQRS.Queries;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.Services;
using Portfolio.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;

namespace Portfolio.Application.Tests.CQRS.Queries;

public class GetPortfolioMetricsQueryHandlerTests
{
    private readonly IAssetMarketDataService _mockMarketData;
    private readonly IPortfolioMetricsCalculator _mockMetrics;
    private readonly InventoryCalculator _inventoryCalculator;

    public GetPortfolioMetricsQueryHandlerTests()
    {
        _mockMarketData = Substitute.For<IAssetMarketDataService>();
        _mockMetrics = Substitute.For<IPortfolioMetricsCalculator>();
        _inventoryCalculator = new InventoryCalculator();
    }

    [Fact]
    public async Task Handle_ShouldOrchestrateFlowCorrectly()
    {
        var mockUow = Substitute.For<IUnitOfWork>();
        
        Guid btcId = Guid.NewGuid();
        Transaction tx = new(DateTime.UtcNow, TransactionType.Swap, FiatCurrency.USD.Id, btcId, 10000, 1, 1, null, 0, null, null, null, null, null, null, null)
        {
            FromAsset = new Asset("USD", "US Dollar", null, null, AssetType.Fiat),
            ToAsset = new Asset("BTC", "Bitcoin", null, null, AssetType.Crypto)
        };

        mockUow.Transactions.GetAllAsync().Returns(Task.FromResult((IEnumerable<Transaction>)[tx]));

        _mockMarketData.GetMarketDataAsync(Arg.Any<IEnumerable<Guid>>(), FiatCurrency.USD)
            .Returns(Task.FromResult(new Dictionary<Guid, AssetMarketData> { { btcId, new AssetMarketData("BTC", "Bitcoin", 30000m) } }));

        _mockMetrics.CalculateMetrics(Arg.Any<List<AssetHolding>>(), Arg.Any<Dictionary<Guid, decimal>>())
            .Returns(new PortfolioMetrics());

        var handler = new GetPortfolioMetricsQueryHandler(
            mockUow, _inventoryCalculator, _mockMarketData, _mockMetrics, NullLogger<GetPortfolioMetricsQueryHandler>.Instance);

        await handler.Handle(new GetPortfolioMetricsQuery(), CancellationToken.None);

        _mockMetrics.Received(1).CalculateMetrics(
            Arg.Is<List<AssetHolding>>(h => h.Count == 1 && h.First().Id == btcId),
            Arg.Any<Dictionary<Guid, decimal>>());
    }
}
