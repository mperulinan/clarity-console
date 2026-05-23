using Portfolio.Application.DTOs;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Application.Mappers;

public static class PortfolioMetricsMapper
{
    public static PortfolioMetricsDto ToDto(this PortfolioMetrics metrics)
    {
        return new PortfolioMetricsDto
        {
            Holdings = metrics.Holdings.Select(h => new EnrichedAssetHoldingDto
            {
                Id = h.Id,
                Symbol = h.Symbol,
                Name = h.Name,
                ImageUrl = h.ImageUrl,
                Quantity = h.Quantity,
                CurrentPrice = h.CurrentPrice,
                CurrentValue = h.CurrentValue,
                AvgCost = h.AvgCost,
                TotalCostBasis = h.TotalCostBasis,
                CostBasisOfSold = h.CostBasisOfSold,
                OpenPL = h.OpenPL,
                OpenReturn = h.OpenReturn,
                RealizedPL = h.RealizedPL,
                RealizedReturn = h.RealizedReturn,
                TotalPL = h.TotalPL,
                TotalReturn = h.TotalReturn,
                AllocationPercentage = h.AllocationPercentage
            }).ToList(),
            TotalPortfolioValueUsd = metrics.TotalPortfolioValueUsd,
            TotalCostBasisUsd = metrics.TotalCostBasisUsd,
            NetDepositsUsd = metrics.NetDepositsUsd,
            TotalUnrealizedProfitLossUsd = metrics.TotalUnrealizedProfitLossUsd,
            TotalRealizedProfitLossUsd = metrics.TotalRealizedProfitLossUsd,
            TotalProfitLossUsd = metrics.TotalProfitLossUsd,
            UnrealizedProfitLossPercentage = metrics.UnrealizedProfitLossPercentage,
            TotalReturn = metrics.TotalReturn
        };
    }
}
