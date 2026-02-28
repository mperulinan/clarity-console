namespace Portfolio.Domain.Entities;

public class Asset
{
    public Guid Id { get; private set; }
    public string Symbol { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    
    public string? ExternalId { get; private set; } 

    public string AssetTypeCode { get; private set; } = null!;
    public AssetType AssetType { get; private set; } = null!;

    // Constructor for EF Core
    private Asset() { }

    public Asset(string symbol, string name, string? externalId, AssetType assetType)
    {
        Id = Guid.NewGuid();
        Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        ExternalId = externalId;
        AssetType = assetType ?? throw new ArgumentNullException(nameof(assetType));
        AssetTypeCode = assetType.Value;
    }
}
