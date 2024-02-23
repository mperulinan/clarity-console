namespace Portfolio.Server.Models.Others
{
    public class AssetWithHoldings
    {
        public required string AssetId { get; set; }
        public decimal Holdings { get; set; }
    }
}
