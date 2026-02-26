namespace Portfolio.Domain.ValueObjects;

public class AssetMarketData(string symbol, string name, decimal price, string? imageUrl = null)
{
    public string Symbol { get; } = symbol;
    public string Name { get; } = name;
    public decimal Price { get; } = price;
    public string? ImageUrl { get; } = imageUrl;
}
