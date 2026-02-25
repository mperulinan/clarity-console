namespace Portfolio.Domain.ValueObjects;

public class AssetMarketData
{
    public decimal Price { get; }
    public string? ImageUrl { get; }

    public AssetMarketData(decimal price, string? imageUrl = null)
    {
        Price = price;
        ImageUrl = imageUrl;
    }
}
