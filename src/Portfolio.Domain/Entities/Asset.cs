using Portfolio.Domain.Enums;

namespace Portfolio.Domain.Entities;

public class Asset
{
    public Guid Id { get; private set; }
    public string Symbol { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    
    public string? ExternalId { get; private set; }
    public string? ImageUrl { get; private set; }

    public AssetType Type { get; private set; } = null!;

    // Constructor for EF Core
    private Asset() { }

    public Asset(string symbol, string name, string? externalId, string? imageUrl, AssetType type)
    {
        Id = Guid.NewGuid();
        Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        ExternalId = externalId;
        ImageUrl = imageUrl;
        Type = type ?? throw new ArgumentNullException(nameof(type));
    }

    public static Asset CreateForSeeding(Guid id, string symbol, string name, string externalId, AssetType type, string? imageUrl = null)
    {
        return new Asset
        {
            Id = id,
            Symbol = symbol,
            Name = name,
            ExternalId = externalId,
            Type = type,
            ImageUrl = imageUrl
        };
    }

    public void UpdateMetadata(string symbol, string name, string? imageUrl)
    {
        Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        ImageUrl = imageUrl;
    }
}
