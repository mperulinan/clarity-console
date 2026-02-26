namespace Portfolio.Domain.ValueObjects;

public class AssetHolding
{
    public string Id { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal AvgCost { get; set; }
    public decimal CostBasisOfSold { get; set; }
    public decimal RealizedPL { get; set; }
}
