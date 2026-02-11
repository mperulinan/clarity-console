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
    private readonly IExchangeRateProvider _mockRates;
    private readonly IAssetPriceService _mockPrices;
    private readonly IPortfolioMetricsCalculator _mockMetrics;
    private readonly InventoryCalculator _inventoryCalculator;
    private readonly PortfolioService _service;

    public PortfolioServiceTests()
    {
        _mockRepo = Substitute.For<ITransactionRepository>();
        _mockRates = Substitute.For<IExchangeRateProvider>();
        _mockPrices = Substitute.For<IAssetPriceService>();
        _mockMetrics = Substitute.For<IPortfolioMetricsCalculator>();
        _inventoryCalculator = new InventoryCalculator();

        _service = new PortfolioService(
            _mockRepo,
            _inventoryCalculator,
            _mockRates,
            _mockPrices,
            _mockMetrics
        );
    }

    [Fact]
    public async Task AddTransaction_ShouldAddTransactionToRepository()
    {
        NewTransactionRequest request = new()
        {
            Date = DateTime.UtcNow,
            TransactionTypeCode = TransactionTypeEnum.Swap,
            FromAssetId = "USD",
            ToAssetId = "BTC",
            AmountSpent = 10000,
            AmountReceived = 1,
            FromAssetPriceInUsd = 1
        };

        await _service.AddTransactionAsync(request);

        await _mockRepo.Received(1).AddAsync(Arg.Is<Transaction>(t => 
            t.FromAssetId == "USD" && 
            t.ToAssetId == "BTC" &&
            t.AmountSpent == 10000
        ));
    }

    [Fact]
    public async Task CalculateExchangeRatesAsync_ShouldUpdateRates_ForPastTransactions()
    {
        var pastDate = DateTime.UtcNow.AddDays(-2);
        Transaction tx = new(pastDate, TransactionTypeEnum.Swap, "USD", "BTC", 100, 1, 1, null, 0, null, null, null, null, null);
        
        // Setup Repo to return this transaction
        _mockRepo.GetAllAsync().Returns(Task.FromResult((IEnumerable<Transaction>)[tx]));
        
        // Setup Exchange Rate Provider
        _mockRates.GetUsdEurRateAsync(Arg.Any<DateTime>()).Returns(Task.FromResult(0.85m));

        await _service.CalculateExchangeRatesAsync();

        await _mockRepo.Received(1).UpdateAsync(Arg.Is<Transaction>(t => 
            t.UsdEurExchangeRate == 0.85m &&
            t.FromAssetPriceInEur == 0.85m // 1 * 0.85
        ));
    }

    [Fact]
    public async Task GetPortfolioMetrics_ShouldOrchestrateFlowCorrectly()
    {
        Transaction tx = new(DateTime.UtcNow, TransactionTypeEnum.Swap, "USD", "BTC", 10000, 1, 1, null, 0, null, null, null, null, null);
        _mockRepo.GetAllAsync().Returns(Task.FromResult((IEnumerable<Transaction>)[tx]));
        
        _mockPrices.GetCurrentPricesAsync(Arg.Any<List<string>>(), FiatCurrency.USD)
            .Returns(Task.FromResult(new Dictionary<string, decimal> { { "BTC", 30000m } }));
            
        _mockMetrics.CalculateMetrics(Arg.Any<List<AssetHolding>>(), Arg.Any<Dictionary<string, decimal>>())
            .Returns(new PortfolioMetrics());

        await _service.GetPortfolioMetricsAsync();

        _mockMetrics.Received(1).CalculateMetrics(
            Arg.Is<List<AssetHolding>>(h => h.Count == 1 && h.First().AssetId == "BTC"), 
            Arg.Any<Dictionary<string, decimal>>());
    }
}
