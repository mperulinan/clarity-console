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
            TotalProfitLossPercentage = totals.TotalReturn
        };
    }
    
    private static List<EnrichedAssetHolding> EnrichHoldings(
        List<AssetHolding> holdings, 
        Dictionary<string, decimal> pricesUsd)
    {
        return [.. holdings.Select(h => 
        {
            decimal currentPrice = pricesUsd.GetValueOrDefault(h.Id.ToLower(), 0);
            decimal currentValue = h.Quantity * currentPrice;
            decimal totalCostBasis = h.Quantity * h.AvgCost;
            decimal openPL = currentValue - totalCostBasis;
            decimal openReturn = h.AvgCost > 0 ? ((currentPrice / h.AvgCost) - 1) * 100 : 0;
            
            decimal realizedReturn = h.CostBasisOfSold > 0 ? h.RealizedPL / h.CostBasisOfSold * 100 : 0;
            
            decimal totalPL = openPL + h.RealizedPL;
            decimal totalInvested = totalCostBasis + h.CostBasisOfSold;
            decimal totalReturn = totalInvested > 0 ? totalPL / totalInvested * 100 : 0;
            
            return new EnrichedAssetHolding
            {
                Id = h.Id,
                Quantity = h.Quantity,
                CurrentPrice = currentPrice,
                CurrentValue = currentValue,
                AvgCost = h.AvgCost,
                TotalCostBasis = totalCostBasis,
                CostBasisOfSold = h.CostBasisOfSold,
                OpenPL = openPL,
                OpenReturn = openReturn,
                RealizedPL = h.RealizedPL,
                RealizedReturn = realizedReturn,
                TotalPL = totalPL,
                TotalReturn = totalReturn,
                AllocationPercentage = 0
            };
        })];
    }
    
    private record PortfolioTotals(
        decimal TotalValue,
        decimal TotalCost,
        decimal TotalUnrealized,
        decimal TotalRealized,
        decimal TotalPL,
        decimal TotalReturn);
    
    private static PortfolioTotals CalculateTotals(List<EnrichedAssetHolding> holdings)
    {
        decimal totalValue = holdings.Sum(h => h.CurrentValue);
        decimal totalCost = holdings.Sum(h => h.TotalCostBasis);
        decimal totalUnrealized = holdings.Sum(h => h.OpenPL);
        decimal totalRealized = holdings.Sum(h => h.RealizedPL);
        decimal totalPL = totalUnrealized + totalRealized;

        decimal totalCostBasisOfSold = holdings.Sum(h => h.CostBasisOfSold);
        decimal totalInvested = totalCost + totalCostBasisOfSold;
        
        decimal totalReturn = totalInvested > 0 ? totalPL / totalInvested * 100 : 0;
        
        return new PortfolioTotals(totalValue, totalCost, totalUnrealized, totalRealized, totalPL, totalReturn);
    }
    
    private static void ApplyAllocations(List<EnrichedAssetHolding> holdings, decimal totalValue)
    {
        if (totalValue > 0)
        {
            foreach (var holding in holdings)
            {
                holding.AllocationPercentage = holding.CurrentValue / totalValue * 100;
            }
        }
    }
}
