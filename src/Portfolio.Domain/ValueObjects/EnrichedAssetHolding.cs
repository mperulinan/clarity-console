namespace Portfolio.Domain.ValueObjects;

public class EnrichedAssetHolding
{
    public string AssetId { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal AvgCost { get; set; }
    public decimal TotalCostBasis { get; set; }
    public decimal CostBasisOfSold { get; set; }
    public decimal OpenPL { get; set; }
    public decimal OpenReturn { get; set; }
    public decimal RealizedPL { get; set; }
    public decimal RealizedReturn { get; set; }
    public decimal TotalPL { get; set; }
    public decimal TotalReturn { get; set; }
    public decimal AllocationPercentage { get; set; }
}
