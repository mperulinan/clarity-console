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
    ITransactionRepository transactionRepository,
    IInventoryCalculator inventoryCalculator,
    IExchangeRateProvider exchangeRateProvider,
    IAssetMarketDataService assetMarketDataService,
    IPortfolioMetricsCalculator portfolioMetricsCalculator,
    ILogger<PortfolioService> logger) : IPortfolioService
{
    private static readonly FiatCurrency DefaultDisplayCurrency = FiatCurrency.USD;
    private static readonly FiatCurrency FinancialReportingCurrency = FiatCurrency.EUR;

    public async Task<PortfolioMetrics> GetPortfolioMetricsAsync()
    {
        var sw = Stopwatch.StartNew();
        logger.LogInformation("Calculating portfolio metrics...");

        // 1. Calculate inventory in USD (explicit).
        var transactions = await transactionRepository.GetAllAsync();
        if (!transactions.Any())
        {
            logger.LogInformation("No transactions found. Returning empty metrics.");
            return new();
        }

        // Metrics are calculated in USD for international performance tracking.
        var report = inventoryCalculator.CalculateInventory(transactions, DefaultDisplayCurrency);

        // 2. Get current market data.
        var assetIds = report.Holdings.Select(h => h.Id).Distinct().ToList();
        var marketData = await assetMarketDataService.GetMarketDataAsync(assetIds, DefaultDisplayCurrency);

        // Extract just prices for the metrics calculator.
        var priceMap = marketData.ToDictionary(x => x.Key, x => x.Value.Price);

        // 3. Calculate metrics.
        var metrics = portfolioMetricsCalculator.CalculateMetrics([.. report.Holdings], priceMap);

        // 4. Enrich holdings with image URLs (from the same market data).
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
        var transactions = await transactionRepository.GetAllAsync();
        
        // Report is generated in EUR for local financial/tax compliance.
        var report = inventoryCalculator.CalculateInventory(transactions, FinancialReportingCurrency);
        
        var dto = new PortfolioReportDto
        {
            Transactions = report.Transactions.Select(pt => new ProcessedTransactionDto
            {
                Transaction = MapToDto(pt.Transaction),
                ProfitLoss = pt.ProfitLoss,
                TotalLossAmount = pt.TotalLossAmount,
                IsLossDisallowed = pt.IsLossDisallowed,
                DisallowedByTransactionId = pt.DisallowedByTransactionId,
                DisallowsPreviousLosses = pt.DisallowsPreviousLosses,
                Error = pt.Error
            }),
            Holdings = report.Holdings.Select(h => new AssetHoldingDto
            {
                Id = h.Id,
                Quantity = h.Quantity,
                AvgCost = h.AvgCost,
                RealizedPL = h.RealizedPL,
                CostBasisOfSold = h.CostBasisOfSold
            })
        };

        logger.LogInformation("Portfolio report generated with {TransactionCount} processed transactions.", 
            dto.Transactions.Count());

        return dto;
    }

    private static TransactionDto MapToDto(Transaction transaction)
    {
        return new TransactionDto
        {
            Id = transaction.Id,
            Date = transaction.Date,
            Type = transaction.Type,
            FromAsset = transaction.FromAsset != null ? transaction.FromAsset.ToDto() : null,
            ToAsset = transaction.ToAsset != null ? transaction.ToAsset.ToDto() : null,
            AmountSpent = transaction.AmountSpent,
            AmountReceived = transaction.AmountReceived,
            SpotPriceUSD = transaction.SpotPriceUSD,
            SpotPriceEUR = transaction.SpotPriceEUR,
            Fee = transaction.Fee,
            FeeAsset = transaction.FeeAsset != null ? transaction.FeeAsset.ToDto() : null,
            FeePriceUSD = transaction.FeePriceUSD,
            FeePriceEUR = transaction.FeePriceEUR,
            UsdEurExchangeRate = transaction.UsdEurExchangeRate,
            Notes = transaction.Notes
        };
    }

    public async Task AddTransactionAsync(NewTransactionRequest request)
    {
        logger.LogInformation("Adding new {TransactionType} transaction dated {Date}...", 
            request.TransactionTypeCode, request.Date.ToString("dd-MM-yyyy"));

        // Store USD prices immediately, EUR prices will be calculated lazily
        // when the Transactions page is loaded (only for past-day transactions)

        var spotCurrency = !string.IsNullOrEmpty(request.SpotPriceInputCurrency) 
            ? FiatCurrency.FromValue(request.SpotPriceInputCurrency.ToLowerInvariant()) 
            : null;

        var feeCurrency = !string.IsNullOrEmpty(request.FeePriceInputCurrency) 
            ? FiatCurrency.FromValue(request.FeePriceInputCurrency.ToLowerInvariant()) 
            : null;

        Transaction newTransaction = new(
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

        await transactionRepository.AddAsync(newTransaction);
        logger.LogInformation("Successfully added transaction {TransactionId}.", newTransaction.Id);
    }

    public async Task CalculateExchangeRatesAsync()
    {
        // This will be called from the Transactions page
        // Only calculates rates for past-day transactions (date < today)
        
        var transactions = await transactionRepository.GetAllAsync();
        var today = DateTime.UtcNow.Date;
        
        // Find transactions that need EUR/USD calculation natively
        var transactionsNeedingRates = transactions.Where(t => 
            t.Date.Date < today && // Only past transactions
            (
                (!t.SpotPriceEUR.HasValue || !t.SpotPriceUSD.HasValue) || 
                (t.FeePriceUSD.HasValue && !t.FeePriceEUR.HasValue) ||
                (t.FeePriceEUR.HasValue && !t.FeePriceUSD.HasValue)
            )
        ).ToList();

        foreach (var transaction in transactionsNeedingRates)
        {
            try
            {
                var rate = await exchangeRateProvider.GetUsdEurRateAsync(transaction.Date);
                
                transaction.UpdateExchangeRates(rate);
                
                await transactionRepository.UpdateAsync(transaction);
            }
            catch (Exception ex)
            {
                // Rate might not be available yet for very recent transactions — non-fatal.
                logger.LogWarning(ex,
                    "Failed to fetch USD/EUR exchange rate for transaction {TransactionId} dated {Date}. It will be retried on the next load.",
                    transaction.Id,
                    transaction.Date.ToString("dd-MM-yyyy"));
            }
        }
    }
}
