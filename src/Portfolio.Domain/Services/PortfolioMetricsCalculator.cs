using Portfolio.Domain.Interfaces;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Services;

public class PortfolioMetricsCalculator : IPortfolioMetricsCalculator
{
    public PortfolioMetrics CalculateMetrics(
        List<AssetHolding> holdings, 
        Dictionary<Guid, decimal> currentPricesUsd)
    {
        List<EnrichedAssetHolding> enriched = EnrichHoldings(holdings, currentPricesUsd);
        PortfolioTotals totals = CalculateTotals(enriched);
        ApplyAllocations(enriched, totals.TotalValue);
        
        return new PortfolioMetrics
        {
            Holdings = enriched,
            TotalPortfolioValueUsd = totals.TotalValue,
            TotalCostBasisUsd = totals.TotalCost,
            NetDepositsUsd = totals.NetDeposits,
            TotalUnrealizedProfitLossUsd = totals.TotalUnrealized,
            TotalRealizedProfitLossUsd = totals.TotalRealized,
            TotalProfitLossUsd = totals.TotalPL,
            UnrealizedProfitLossPercentage = totals.UnrealizedReturn,
            TotalReturn = totals.TotalReturn
        };
    }
    
    private static List<EnrichedAssetHolding> EnrichHoldings(
        List<AssetHolding> holdings, 
        Dictionary<Guid, decimal> pricesUsd)
    {
        return [.. holdings.Select(h => 
        {
            decimal currentPrice = pricesUsd.GetValueOrDefault(h.Id, 0);
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
                AllocationPercentage = 0
            };
        })];
    }
    private record PortfolioTotals(
        decimal TotalValue,
        decimal TotalCost,
        decimal NetDeposits,
        decimal TotalUnrealized,
        decimal TotalRealized,
        decimal TotalPL,
        decimal UnrealizedReturn,
        decimal TotalReturn);
    
    private static PortfolioTotals CalculateTotals(List<EnrichedAssetHolding> holdings)
    {
        decimal totalValue = holdings.Sum(h => h.CurrentValue);
        decimal totalCost = holdings.Sum(h => h.TotalCostBasis);
        decimal totalUnrealized = holdings.Sum(h => h.OpenPL);
        decimal totalRealized = holdings.Sum(h => h.RealizedPL);
        
        // Exact accounting identity for closed-system Net Deposits (Total Fiat In - Total Fiat Out)
        // Since TotalCost = TotalIn - CostOfSold, and RealizedPL = TotalOut - CostOfSold,
        // NetDeposits (TotalIn - TotalOut) = TotalCost - RealizedPL.
        decimal netDeposits = totalCost - totalRealized;
        
        decimal totalPL = totalUnrealized + totalRealized;

        // Unrealized Return: open gain vs. remaining cost basis (what you still hold)
        decimal unrealizedReturn = totalCost > 0 ? totalUnrealized / totalCost * 100 : 0;
        
        // Total Return: all gains vs. net deposited capital (all time)
        decimal totalReturn = netDeposits > 0 ? totalPL / netDeposits * 100 : 0;

        return new PortfolioTotals(totalValue, totalCost, netDeposits, totalUnrealized, totalRealized, totalPL, unrealizedReturn, totalReturn);
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
