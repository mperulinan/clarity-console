using NSubstitute;
using Portfolio.Application.CQRS.Queries;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Portfolio.Application.Tests.CQRS.Queries;

public class GetPortfolioReportQueryHandlerTests
{
    private readonly InventoryCalculator _inventoryCalculator;

    public GetPortfolioReportQueryHandlerTests()
    {
        _inventoryCalculator = new InventoryCalculator();
    }

    [Fact]
    public async Task Handle_ShouldGenerateReport()
    {
        var mockUow = Substitute.For<IUnitOfWork>();
        
        Guid btcId = Guid.NewGuid();
        Transaction tx = new(DateTime.UtcNow, TransactionType.Swap, FiatCurrency.EUR.Id, btcId, 10000, 1, null, 10000m, 0, null, null, null, null, FiatCurrency.EUR, null, null)
        {
            FromAsset = new Asset("EUR", "Euro", null, null, AssetType.Fiat),
            ToAsset = new Asset("BTC", "Bitcoin", null, null, AssetType.Crypto)
        };

        mockUow.Transactions.GetAllAsync().Returns(Task.FromResult((IEnumerable<Transaction>)[tx]));

        var handler = new GetPortfolioReportQueryHandler(
            mockUow, _inventoryCalculator, NullLogger<GetPortfolioReportQueryHandler>.Instance);

        var result = await handler.Handle(new GetPortfolioReportQuery(), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("EUR", result.ReportingCurrency);
        Assert.Single(result.Transactions);
        Assert.Single(result.Holdings);
    }
}
