using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.Services;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Application.CQRS.Queries;

public class GetPortfolioMetricsQueryHandler(
    IUnitOfWork unitOfWork,
    IInventoryCalculator inventoryCalculator,
    IAssetMarketDataService assetMarketDataService,
    IPortfolioMetricsCalculator portfolioMetricsCalculator,
    ILogger<GetPortfolioMetricsQueryHandler> logger) 
    : IRequestHandler<GetPortfolioMetricsQuery, PortfolioMetrics>
{
    private static readonly FiatCurrency DefaultDisplayCurrency = FiatCurrency.USD;

    public async Task<PortfolioMetrics> Handle(GetPortfolioMetricsQuery query, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        logger.LogInformation("Calculating portfolio metrics...");

        var transactions = await unitOfWork.Transactions.GetAllAsync();
        if (!transactions.Any())
        {
            logger.LogInformation("No transactions found. Returning empty metrics.");
            return new();
        }

        var report = inventoryCalculator.CalculateInventory(transactions, DefaultDisplayCurrency);
        var assetIds = report.Holdings.Select(h => h.Id).Distinct().ToList();
        var marketData = await assetMarketDataService.GetMarketDataAsync(assetIds, DefaultDisplayCurrency);
        var priceMap = marketData.ToDictionary(x => x.Key, x => x.Value.Price);
        var metrics = portfolioMetricsCalculator.CalculateMetrics([.. report.Holdings], priceMap);

        EnrichHoldingsMetadata(metrics.Holdings, marketData);

        sw.Stop();
        logger.LogInformation("Portfolio metrics calculated in {ElapsedMs}ms for {HoldingsCount} holdings.",
            sw.ElapsedMilliseconds, metrics.Holdings.Count());

        return metrics;
    }

    private static void EnrichHoldingsMetadata(IEnumerable<EnrichedAssetHolding> holdings, Dictionary<Guid, AssetMarketData> marketData)
    {
        foreach (var holding in holdings)
        {
            if (marketData.TryGetValue(holding.Id, out AssetMarketData? data))
            {
                holding.Symbol = data.Symbol;
                holding.Name = data.Name;
                holding.ImageUrl = data.ImageUrl;
            }
        }
    }
}
