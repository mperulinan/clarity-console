using NSubstitute;
using Portfolio.Application.DTOs;
using Portfolio.Application.Services;
using Portfolio.Application.Tests.Fakes;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.Services;
using Portfolio.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;

namespace Portfolio.Application.Tests;

public class PortfolioServiceTests
{
    private readonly IExchangeRateProvider _mockRates;
    private readonly IAssetMarketDataService _mockMarketData;
    private readonly IPortfolioMetricsCalculator _mockMetrics;
    private readonly InventoryCalculator _inventoryCalculator;

    public PortfolioServiceTests()
    {
        _mockRates = Substitute.For<IExchangeRateProvider>();
        _mockMarketData = Substitute.For<IAssetMarketDataService>();
        _mockMetrics = Substitute.For<IPortfolioMetricsCalculator>();
        _inventoryCalculator = new InventoryCalculator();
    }

    private PortfolioService BuildService(IUnitOfWork uow) => new(
        uow, _inventoryCalculator, _mockRates, _mockMarketData, _mockMetrics,
        NullLogger<PortfolioService>.Instance);

    private static Transaction CreateTx(
        DateTime? date = null,
        TransactionType? type = null,
        string? fromAsset = null,
        string? toAsset = null,
        decimal spent = 0,
        decimal received = 0,
        decimal spotPriceUSD = 1,
        decimal? spotPriceEUR = null,
        decimal fee = 0,
        string? feeAsset = null,
        decimal? feeUsdPrice = null,
        decimal? feeEurPrice = null,
        decimal? xr = null,
        FiatCurrency? spotCurrency = null,
        FiatCurrency? feeCurrency = null,
        string? notes = null)
    {
        return new Transaction(
            date ?? DateTime.UtcNow,
            type ?? TransactionType.Swap,
            Guid.NewGuid(),
            Guid.NewGuid(),
            spent,
            received,
            spotPriceUSD,
            spotPriceEUR,
            fee,
            feeAsset != null ? Guid.NewGuid() : null,
            feeUsdPrice,
            feeEurPrice,
            xr,
            spotCurrency ?? (spotPriceEUR.HasValue && spotPriceUSD == 0 ? FiatCurrency.EUR : FiatCurrency.USD),
            feeCurrency,
            notes)
        {
            FromAsset = new Asset(fromAsset ?? "USD", fromAsset ?? "USD", null, null, AssetType.Fiat),
            ToAsset = new Asset(toAsset ?? "BTC", toAsset ?? "BTC", null, null, AssetType.Fiat)
        };
    }

    // -------------------------------------------------------------------------
    // Write Paths — verified via FakeUnitOfWork (no mock chains, no EF Core)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AddTransaction_ShouldStageAndPersistTransaction()
    {
        var uow = new FakeUnitOfWork();
        var service = BuildService(uow);

        NewTransactionRequest request = new()
        {
            Date = DateTime.UtcNow,
            TransactionTypeCode = TransactionType.Swap,
            FromAssetId = Guid.NewGuid(),
            ToAssetId = Guid.NewGuid(),
            AmountSpent = 10000,
            AmountReceived = 1,
            SpotPriceUSD = 1,
            SpotPriceInputCurrency = "USD"
        };

        await service.AddTransactionAsync(request);

        var saved = (await uow.FakeTransactions.GetAllAsync()).Single();
        Assert.Equal(request.FromAssetId, saved.FromAssetId);
        Assert.Equal(request.ToAssetId, saved.ToAssetId);
        Assert.Equal(10000, saved.AmountSpent);
        Assert.Equal(1, uow.SaveChangesCallCount); // persisted exactly once
    }

    [Fact]
    public async Task CalculateExchangeRatesAsync_ShouldUpdateRates_ForPastTransactions()
    {
        var uow = new FakeUnitOfWork();
        var pastDate = DateTime.UtcNow.AddDays(-2);
        var tx = CreateTx(pastDate, TransactionType.Swap, "USD", "BTC", 100, 1, 1);
        await uow.FakeTransactions.AddAsync(tx);

        _mockRates.GetUsdEurRateAsync(Arg.Any<DateTime>()).Returns(Task.FromResult(0.85m));

        var service = BuildService(uow);
        await service.CalculateExchangeRatesAsync();

        Assert.Equal(0.85m, tx.UsdEurExchangeRate);
        Assert.Equal(0.85m, tx.SpotPriceEUR);
        Assert.Equal(1, uow.SaveChangesCallCount); // all updates in one batch
    }

    [Fact]
    public async Task CalculateExchangeRatesAsync_ShouldBatchSave_WhenMultiplePendingTransactions()
    {
        var uow = new FakeUnitOfWork();
        var pastDate = DateTime.UtcNow.AddDays(-2);

        // Three pending transactions
        for (int i = 0; i < 3; i++)
            await uow.FakeTransactions.AddAsync(CreateTx(pastDate, TransactionType.Swap, "USD", "BTC", 100, 1, 1));

        _mockRates.GetUsdEurRateAsync(Arg.Any<DateTime>()).Returns(Task.FromResult(0.9m));

        var service = BuildService(uow);
        await service.CalculateExchangeRatesAsync();

        // Only 1 SaveChangesAsync call regardless of how many transactions were updated
        Assert.Equal(1, uow.SaveChangesCallCount);
    }

    [Fact]
    public async Task CalculateExchangeRatesAsync_ShouldUpdateUsdFromEur_WhenEurIsTruthSource()
    {
        var uow = new FakeUnitOfWork();
        var pastDate = DateTime.UtcNow.AddDays(-3);
        var tx = new Transaction(pastDate, TransactionType.Swap, Guid.NewGuid(), Guid.NewGuid(), 1, 1, null, 100m, 0, null, null, null, null, FiatCurrency.EUR, null, null)
        {
            FromAsset = new Asset("EUR", "Euro", null, null, AssetType.Fiat),
            ToAsset = new Asset("BTC", "Bitcoin", null, null, AssetType.Crypto)
        };
        await uow.FakeTransactions.AddAsync(tx);

        _mockRates.GetUsdEurRateAsync(pastDate).Returns(Task.FromResult(0.8m)); // 1 USD = 0.8 EUR → 1 EUR = 1.25 USD

        var service = BuildService(uow);
        await service.CalculateExchangeRatesAsync();

        Assert.Equal(125m, tx.SpotPriceUSD); // 100 / 0.8
        Assert.Equal(100m, tx.SpotPriceEUR);
    }

    // -------------------------------------------------------------------------
    // Read Paths — mock IUnitOfWork at the top level only (no mock chains)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task GetPortfolioMetrics_ShouldOrchestrateFlowCorrectly()
    {
        var mockUow = Substitute.For<IUnitOfWork>();
        var service = BuildService(mockUow);

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

        await service.GetPortfolioMetricsAsync();

        _mockMetrics.Received(1).CalculateMetrics(
            Arg.Is<List<AssetHolding>>(h => h.Count == 1 && h.First().Id == btcId),
            Arg.Any<Dictionary<Guid, decimal>>());
    }
}
