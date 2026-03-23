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
    IPortfolioMetricsCalculator portfolioMetricsCalculator) : IPortfolioService
{
    public async Task<PortfolioMetrics> GetPortfolioMetricsAsync()
    {
        // 1. Calculate inventory in USD (explicit).
        var transactions = await transactionRepository.GetAllAsync();
        if (!transactions.Any())
        {
            return new();
        }

        var report = inventoryCalculator.CalculateInventory(transactions, FiatCurrency.USD);

        // 2. Get current USD market data.
        var assetIds = report.Holdings.Select(h => h.Id).Distinct().ToList();
        var marketData = await assetMarketDataService.GetMarketDataAsync(assetIds, FiatCurrency.USD);

        // Extract just prices for the metrics calculator.
        var priceMap = marketData.ToDictionary(x => x.Key, x => x.Value.Price);

        // 3. Calculate metrics.
        var metrics = portfolioMetricsCalculator.CalculateMetrics([.. report.Holdings], priceMap);

        // 4. Enrich holdings with image URLs (from the same market data).
        EnrichHoldingsMetadata(metrics.Holdings, marketData);

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
        var transactions = await transactionRepository.GetAllAsync();
        var report = inventoryCalculator.CalculateInventory(transactions, FiatCurrency.EUR);
        
        return new PortfolioReportDto
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
        // Store USD prices immediately, EUR prices will be calculated lazily
        // when the Transactions page is loaded (only for past-day transactions)

        var spotCurrency = !string.IsNullOrEmpty(request.SpotPriceInputCurrency) 
            ? FiatCurrency.FromValue(request.SpotPriceInputCurrency.ToLowerInvariant()) 
            : null;

        var feeCurrency = !string.IsNullOrEmpty(request.FeePriceInputCurrency) 
            ? FiatCurrency.FromValue(request.FeePriceInputCurrency.ToLowerInvariant()) 
            : null;

        Transaction newTransaction = new(
            request.Date.ToUniversalTime(),
            request.TransactionTypeCode,
            request.FromAssetId,
            request.ToAssetId,
            request.AmountSpent,
            request.AmountReceived,
            request.SpotPriceUSD,
            request.SpotPriceEUR,
            request.Fee,
            request.FeeAssetId,
            request.FeePriceUSD,
            request.FeePriceEUR,
            null, // UsdEurExchangeRate - will be set when EUR prices are calculated
            spotCurrency,
            feeCurrency,
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
                // Log but don't fail - some rates might not be available yet
                Console.WriteLine($"Could not fetch rate for {transaction.Date:yyyy-MM-dd}: {ex.Message}");
            }
        }
    }
}
