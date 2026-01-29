namespace Portfolio.Domain.ValueObjects;

public class AssetHolding
{
    public string AssetId { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal AvgCost { get; set; }
    public decimal RealizedProfitLoss { get; set; }
}
