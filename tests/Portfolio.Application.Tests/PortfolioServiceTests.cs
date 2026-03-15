using NSubstitute;
using Portfolio.Application.DTOs;
using Portfolio.Application.Services;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.Services;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Application.Tests;

public class PortfolioServiceTests
{
    private readonly ITransactionRepository _mockRepo;
    private readonly IAssetRepository _mockAssetRepo;
    private readonly IExchangeRateProvider _mockRates;
    private readonly IAssetMarketDataService _mockMarketData;
    private readonly IPortfolioMetricsCalculator _mockMetrics;
    private readonly InventoryCalculator _inventoryCalculator;
    private readonly PortfolioService _service;

    public PortfolioServiceTests()
    {
        _mockRepo = Substitute.For<ITransactionRepository>();
        _mockAssetRepo = Substitute.For<IAssetRepository>();
        _mockRates = Substitute.For<IExchangeRateProvider>();
        _mockMarketData = Substitute.For<IAssetMarketDataService>();
        _mockMetrics = Substitute.For<IPortfolioMetricsCalculator>();
        _inventoryCalculator = new InventoryCalculator();

        _service = new PortfolioService(
            _mockRepo,
            _inventoryCalculator,
            _mockRates,
            _mockMarketData,
            _mockMetrics
        );
    }

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

    [Fact]
    public async Task AddTransaction_ShouldAddTransactionToRepository()
    {
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

        await _service.AddTransactionAsync(request);

        await _mockRepo.Received(1).AddAsync(Arg.Is<Transaction>(t => 
            t.FromAssetId == request.FromAssetId && 
            t.ToAssetId == request.ToAssetId &&
            t.AmountSpent == 10000
        ));
    }

    [Fact]
    public async Task CalculateExchangeRatesAsync_ShouldUpdateRates_ForPastTransactions()
    {
        var pastDate = DateTime.UtcNow.AddDays(-2);
        Transaction tx = CreateTx(pastDate, TransactionType.Swap, "USD", "BTC", 100, 1, 1, null, 0, null, null, null, null, null);
        
        // Setup Repo to return this transaction
        _mockRepo.GetAllAsync().Returns(Task.FromResult((IEnumerable<Transaction>)[tx]));
        
        // Setup Exchange Rate Provider
        _mockRates.GetUsdEurRateAsync(Arg.Any<DateTime>()).Returns(Task.FromResult(0.85m));

        await _service.CalculateExchangeRatesAsync();

        await _mockRepo.Received(1).UpdateAsync(Arg.Is<Transaction>(t => 
            t.UsdEurExchangeRate == 0.85m &&
            t.SpotPriceEUR == 0.85m // 1 * 0.85
        ));
    }

    [Fact]
    public async Task CalculateExchangeRatesAsync_ShouldUpdateUsdFromEur_WhenEurIsTruthSource()
    {
        var pastDate = DateTime.UtcNow.AddDays(-3);
        // Start with 100 EUR, Truth source is EUR.
        Transaction tx = new(pastDate, TransactionType.Swap, Guid.NewGuid(), Guid.NewGuid(), 1, 1, null, 100m, 0, null, null, null, null, FiatCurrency.EUR, null, null)
        {
            FromAsset = new Asset("EUR", "Euro", null, null, AssetType.Fiat),
            ToAsset = new Asset("BTC", "Bitcoin", null, null, AssetType.Crypto)
        };
        
        _mockRepo.GetAllAsync().Returns(Task.FromResult((IEnumerable<Transaction>)[tx]));
        _mockRates.GetUsdEurRateAsync(pastDate).Returns(Task.FromResult(0.8m)); // 1 USD = 0.8 EUR -> 1 EUR = 1.25 USD

        await _service.CalculateExchangeRatesAsync();

        await _mockRepo.Received(1).UpdateAsync(Arg.Is<Transaction>(t => 
            t.SpotPriceUSD == 125m && // (100 / 0.8)
            t.SpotPriceEUR == 100m
        ));
    }

    [Fact]
    public async Task GetPortfolioMetrics_ShouldOrchestrateFlowCorrectly()
    {
        Guid btcId = Guid.NewGuid();
        Transaction tx = new(DateTime.UtcNow, TransactionType.Swap, Guid.NewGuid(), btcId, 10000, 1, 1, null, 0, null, null, null, null, null, null, null)
        {
            FromAsset = new Asset("USD", "US Dollar", null, null, AssetType.Fiat),
            ToAsset = new Asset("BTC", "Bitcoin", null, null, AssetType.Crypto)
        };
        _mockRepo.GetAllAsync().Returns(Task.FromResult((IEnumerable<Transaction>)[tx]));
        
        _mockMarketData.GetMarketDataAsync(Arg.Any<IEnumerable<Guid>>(), FiatCurrency.USD)
            .Returns(Task.FromResult(new Dictionary<Guid, AssetMarketData> { { btcId, new AssetMarketData("BTC", "Bitcoin", 30000m) } }));
            
        _mockMetrics.CalculateMetrics(Arg.Any<List<AssetHolding>>(), Arg.Any<Dictionary<Guid, decimal>>())
            .Returns(new PortfolioMetrics());

        await _service.GetPortfolioMetricsAsync();

        _mockMetrics.Received(1).CalculateMetrics(
            Arg.Is<List<AssetHolding>>(h => h.Count == 1 && h.First().Id == btcId), 
            Arg.Any<Dictionary<Guid, decimal>>());
    }
}
