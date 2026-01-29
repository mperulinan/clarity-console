namespace Portfolio.Domain.ValueObjects;

public class PortfolioMetrics
{
    public List<EnrichedAssetHolding> Holdings { get; set; } = new();
    public decimal TotalPortfolioValueUsd { get; set; }
    public decimal TotalCostBasisUsd { get; set; }
    public decimal TotalUnrealizedProfitLossUsd { get; set; }
    public decimal TotalRealizedProfitLossUsd { get; set; }
    public decimal TotalProfitLossUsd { get; set; }
    public decimal TotalProfitLossPercentage { get; set; }
}
