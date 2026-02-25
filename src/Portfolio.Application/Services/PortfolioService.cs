using Portfolio.Application.Interfaces;
using Portfolio.Application.DTOs;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.ValueObjects;
using Portfolio.Domain.Enums;

namespace Portfolio.Application.Services;

public class PortfolioService(
    ITransactionRepository transactionRepository, 
    IInventoryCalculator inventoryCalculator,
    IExchangeRateProvider exchangeRateProvider,
    IAssetMarketDataService assetMarketDataService,
    IPortfolioMetricsCalculator portfolioMetricsCalculator) : IPortfolioService
{
    public async Task<PortfolioMetrics> GetPortfolioMetricsAsync()
    {
        // 1. Calculate inventory in USD (explicit)
        var transactions = await transactionRepository.GetAllAsync();
        var report = inventoryCalculator.CalculateInventory(
            transactions, 
            FiatCurrency.USD); // Explicitly USD
        
        // 2. Get current USD market data
        var assetIds = report.Holdings.Select(h => h.AssetId).Distinct().ToList();
        var marketDataUsd = await assetMarketDataService.GetMarketDataAsync(assetIds, FiatCurrency.USD);
        
        // Extract just prices for the metrics calculator
        var pricesUsd = marketDataUsd.ToDictionary(k => k.Key, v => v.Value.Price);

        // 3. Calculate metrics
        var metrics = portfolioMetricsCalculator.CalculateMetrics(
            [.. report.Holdings], 
            pricesUsd);

        // 4. Enrich holdings with image URLs (from the same market data)
        foreach (var holding in metrics.Holdings)
        {
            if (marketDataUsd.TryGetValue(holding.AssetId, out var data) && data.ImageUrl != null)
            {
                holding.ImageUrl = data.ImageUrl;
            }
        }
        
        return metrics;
    }
    
    // Kept for backward compatibility / Tax Report, but explicitly EUR
    public async Task<PortfolioReport> GetPortfolioReportAsync() 
    {
        var transactions = await transactionRepository.GetAllAsync();
        return inventoryCalculator.CalculateInventory(transactions, FiatCurrency.EUR);
    }

    public async Task AddTransactionAsync(NewTransactionRequest request)
    {
        // Store USD prices immediately, EUR prices will be calculated lazily
        // when the Transactions page is loaded (only for past-day transactions)
        
        var newTransaction = new Transaction(
            request.Date.ToUniversalTime(),
            request.TransactionTypeCode,
            request.FromAssetId,
            request.ToAssetId,
            request.AmountSpent,
            request.AmountReceived,
            request.FromAssetPriceInUsd,
            request.FromAssetPriceInEur,
            request.Fee,
            request.FeeAsset,
            request.FeeAssetPriceInUsd,
            request.FeeAssetPriceInEur,
            null, // UsdEurExchangeRate - will be set when EUR prices are calculated
            request.Notes
        );

        await transactionRepository.AddAsync(newTransaction);
    }

    public async Task CalculateExchangeRatesAsync()
    {
        // This will be called from the Transactions page
        // Only calculates rates for past-day transactions (date < today)
        
        var transactions = await transactionRepository.GetAllAsync();
        var today = DateTime.UtcNow.Date;
        
        // Find transactions that need EUR calculation
        var transactionsNeedingRates = transactions.Where(t => 
            t.Date.Date < today && // Only past transactions
            (
                (!t.FromAssetPriceInEur.HasValue) || (t.FeeAssetPriceInUsd.HasValue && !t.FeeAssetPriceInEur.HasValue)
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
                // Log but don't fail - some rates might not be available yet
                Console.WriteLine($"Could not fetch rate for {transaction.Date:yyyy-MM-dd}: {ex.Message}");
            }
        }
    }
}
