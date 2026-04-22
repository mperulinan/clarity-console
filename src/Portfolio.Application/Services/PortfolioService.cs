using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using Portfolio.Application.Mappers;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.Services;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Application.Services;

public class PortfolioService(
    IUnitOfWork unitOfWork,
    IInventoryCalculator inventoryCalculator,
    IExchangeRateProvider exchangeRateProvider,
    IAssetMarketDataService assetMarketDataService,
    IPortfolioMetricsCalculator portfolioMetricsCalculator,
    ILogger<PortfolioService> logger) : IPortfolioService
{
    // Metrics are calculated in USD for international performance tracking.
    private static readonly FiatCurrency DefaultDisplayCurrency = FiatCurrency.USD;

    // Reports are generated in EUR for local financial/tax compliance.
    private static readonly FiatCurrency FinancialReportingCurrency = FiatCurrency.EUR;

    public async Task<PortfolioMetrics> GetPortfolioMetricsAsync()
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

    public async Task<PortfolioReportDto> GetPortfolioReportAsync()
    {
        logger.LogInformation("Generating portfolio report ({Currency})...", FinancialReportingCurrency.Value);

        var transactions = await unitOfWork.Transactions.GetAllAsync();
        var report = inventoryCalculator.CalculateInventory(transactions, FinancialReportingCurrency);
        var processedList = report.Transactions.ToList();

        var yearSummaries = processedList
            .GroupBy(pt => pt.Transaction.Date.Year)
            .Select(g =>
            {
                decimal totalGains = 0, totalLosses = 0, disallowed = 0;
                int errorCount = 0;

                foreach (var pt in g)
                {
                    if (pt.ProfitLoss.HasValue)
                    {
                        if (pt.IsLossDisallowed)
                        {
                            disallowed += Math.Abs(pt.ProfitLoss.Value);
                        }
                        else if (pt.ProfitLoss.Value >= 0)
                        {
                            totalGains += pt.ProfitLoss.Value;
                        }
                        else
                        {
                            totalLosses += pt.ProfitLoss.Value;
                        }
                    }

                    if (pt.Error != null) errorCount++;
                }

                return new YearSummaryDto
                {
                    Year = g.Key,
                    TotalGains = totalGains,
                    TotalLosses = totalLosses,
                    NetPL = totalGains + totalLosses,
                    DisallowedLosses = disallowed,
                    EventCount = g.Count(),
                    ErrorCount = errorCount
                };
            })
            .OrderByDescending(ys => ys.Year)
            .ToList();

        var dto = new PortfolioReportDto
        {
            ReportingCurrency = FinancialReportingCurrency.Value.ToUpperInvariant(),
            Transactions = processedList.Select(pt => pt.ToDto()),
            Holdings = report.Holdings.Select(h => new AssetHoldingDto
            {
                Id = h.Id,
                Quantity = h.Quantity,
                AvgCost = h.AvgCost,
                RealizedPL = h.RealizedPL,
                CostBasisOfSold = h.CostBasisOfSold
            }),
            YearSummaries = yearSummaries
        };

        logger.LogInformation("Portfolio report generated with {TransactionCount} processed transactions.",
            processedList.Count);

        return dto;
    }

    public async Task AddTransactionAsync(NewTransactionRequest request)
    {
        logger.LogInformation("Adding new {TransactionType} transaction dated {Date}...",
            request.TransactionTypeCode, request.Date.ToString("dd-MM-yyyy"));

        var transaction = BuildTransaction(request);
        await unitOfWork.Transactions.AddAsync(transaction);
        await unitOfWork.SaveChangesAsync();

        logger.LogInformation("Successfully added transaction {TransactionId}.", transaction.Id);
    }

    private static Transaction BuildTransaction(NewTransactionRequest request)
    {
        var spotCurrency = ParseCurrency(request.SpotPriceInputCurrency);
        var feeCurrency = ParseCurrency(request.FeePriceInputCurrency);

        return new Transaction(
            date: request.Date.ToUniversalTime(),
            transactionType: request.TransactionTypeCode,
            fromAssetId: request.FromAssetId,
            toAssetId: request.ToAssetId,
            amountSpent: request.AmountSpent,
            amountReceived: request.AmountReceived,
            spotPriceUSD: request.SpotPriceUSD,
            spotPriceEUR: request.SpotPriceEUR,
            fee: request.Fee,
            feeAssetId: request.FeeAssetId,
            feeSpotPriceUSD: request.FeePriceUSD,
            feeSpotPriceEUR: request.FeePriceEUR,
            usdEurExchangeRate: null, // Calculated lazily for past-day transactions
            spotPriceInputCurrency: spotCurrency,
            feePriceInputCurrency: feeCurrency,
            notes: request.Notes
        );
    }

    private static FiatCurrency? ParseCurrency(string? currencyCode) =>
        !string.IsNullOrEmpty(currencyCode)
            ? FiatCurrency.FromValue(currencyCode.ToLowerInvariant())
            : null;

    public async Task CalculateExchangeRatesAsync()
    {
        var transactions = await unitOfWork.Transactions.GetAllAsync();
        var pending = transactions.Where(t => t.Date.Date < DateTime.UtcNow.Date && t.HasIncompleteExchangeRates).ToList();

        logger.LogInformation("Found {Count} transactions pending exchange rate calculation.", pending.Count);

        foreach (var transaction in pending)
        {
            await TryUpdateExchangeRateAsync(transaction);
        }

        if (pending.Count > 0)
        {
            await unitOfWork.SaveChangesAsync();
        }
    }

    private async Task TryUpdateExchangeRateAsync(Transaction transaction)
    {
        try
        {
            var rate = await exchangeRateProvider.GetUsdEurRateAsync(transaction.Date);
            transaction.UpdateExchangeRates(rate);
            await unitOfWork.Transactions.UpdateAsync(transaction);
        }
        catch (Exception ex)
        {
            // Rate might not be available yet for very recent transactions — non-fatal.
            logger.LogWarning(ex,
                "Failed to fetch USD/EUR rate for transaction {TransactionId} on {Date}. Will retry on next load.",
                transaction.Id,
                transaction.Date.ToString("dd-MM-yyyy"));
        }
    }
}
