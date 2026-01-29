namespace Portfolio.Domain.ValueObjects;

public class EnrichedAssetHolding
{
    public string AssetId { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal CurrentPriceUsd { get; set; }
    public decimal CurrentValueUsd { get; set; }
    public decimal AvgCostUsd { get; set; }
    public decimal TotalCostBasisUsd { get; set; }
    public decimal UnrealizedProfitLossUsd { get; set; }
    public decimal RealizedProfitLossUsd { get; set; }
    public decimal TotalProfitLossUsd { get; set; }
    public decimal YieldPercentage { get; set; }
    public decimal AllocationPercentage { get; set; }
}
