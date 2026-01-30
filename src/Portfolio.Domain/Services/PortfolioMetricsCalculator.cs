using Portfolio.Domain.Interfaces;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Services;

public class PortfolioMetricsCalculator : IPortfolioMetricsCalculator
{
    public PortfolioMetrics CalculateMetrics(
        List<AssetHolding> holdings, 
        Dictionary<string, decimal> currentPricesUsd)
    {
        List<EnrichedAssetHolding> enriched = EnrichHoldings(holdings, currentPricesUsd);
        PortfolioTotals totals = CalculateTotals(enriched);
        ApplyAllocations(enriched, totals.TotalValue);
        
        return new PortfolioMetrics
        {
            Holdings = enriched,
            TotalPortfolioValueUsd = totals.TotalValue,
            TotalCostBasisUsd = totals.TotalCost,
            TotalUnrealizedProfitLossUsd = totals.TotalUnrealized,
            TotalRealizedProfitLossUsd = totals.TotalRealized,
            TotalProfitLossUsd = totals.TotalPL,
            TotalProfitLossPercentage = totals.PLPercentage
        };
    }
    
    private List<EnrichedAssetHolding> EnrichHoldings(
        List<AssetHolding> holdings, 
        Dictionary<string, decimal> pricesUsd)
    {
        return holdings.Select(h => 
        {
            decimal currentPrice = pricesUsd.GetValueOrDefault(h.AssetId.ToLower(), 0);
            decimal currentValue = h.Quantity * currentPrice;
            decimal totalCost = h.Quantity * h.AvgCost; // AvgCost already in USD based on inventory calculation
            decimal unrealizedPL = currentValue - totalCost;
            decimal totalPL = unrealizedPL + h.RealizedProfitLoss; // Already in USD
            decimal yieldPct = totalCost > 0 ? (totalPL / totalCost) * 100 : 0;
            
            return new EnrichedAssetHolding
            {
                AssetId = h.AssetId,
                Quantity = h.Quantity,
                CurrentPriceUsd = currentPrice,
                CurrentValueUsd = currentValue,
                AvgCostUsd = h.AvgCost,
                TotalCostBasisUsd = totalCost,
                UnrealizedProfitLossUsd = unrealizedPL,
                RealizedProfitLossUsd = h.RealizedProfitLoss,
                TotalProfitLossUsd = totalPL,
                YieldPercentage = yieldPct,
                AllocationPercentage = 0
            };
        }).ToList();
    }
    
    private record PortfolioTotals(
        decimal TotalValue,
        decimal TotalCost,
        decimal TotalUnrealized,
        decimal TotalRealized,
        decimal TotalPL,
        decimal PLPercentage);
    
    private PortfolioTotals CalculateTotals(List<EnrichedAssetHolding> holdings)
    {
        decimal totalValue = holdings.Sum(h => h.CurrentValueUsd);
        decimal totalCost = holdings.Sum(h => h.TotalCostBasisUsd);
        decimal totalUnrealized = holdings.Sum(h => h.UnrealizedProfitLossUsd);
        decimal totalRealized = holdings.Sum(h => h.RealizedProfitLossUsd);
        decimal totalPL = totalUnrealized + totalRealized;
        decimal plPct = totalCost > 0 ? (totalPL / totalCost) * 100 : 0;
        
        return new PortfolioTotals(totalValue, totalCost, totalUnrealized, totalRealized, totalPL, plPct);
    }
    
    private void ApplyAllocations(List<EnrichedAssetHolding> holdings, decimal totalValue)
    {
        if (totalValue > 0)
        {
            foreach (var holding in holdings)
            {
                holding.AllocationPercentage = (holding.CurrentValueUsd / totalValue) * 100;
            }
        }
    }
}
