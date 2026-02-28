using Ardalis.SmartEnum;

namespace Portfolio.Domain.Entities;

public class AssetType : SmartEnum<AssetType, string>
{
    public static readonly AssetType Crypto = new("CRYPTO", "Crypto");
    public static readonly AssetType Fiat = new("FIAT", "Fiat");
    public static readonly AssetType Stock = new("STOCK", "Stock");

    // Navigation
    public ICollection<Asset> Assets { get; private set; } = [];

    // Constructor for EF Core
    private AssetType() : base("Crypto", "CRYPTO") { }

    private AssetType(string value, string name)
        : base(name, value)
    {
    }

    public bool IsCrypto() => this == Crypto;
}