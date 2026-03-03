using Ardalis.SmartEnum;
using Portfolio.Domain.Entities;

namespace Portfolio.Domain.ValueObjects;

public class AssetType : SmartEnum<AssetType, string>
{
    public static readonly AssetType Crypto = new("CRYPTO", "Crypto");
    public static readonly AssetType Fiat = new("FIAT", "Fiat");
    public static readonly AssetType Stock = new("STOCK", "Stock");
    public static readonly AssetType Other = new("OTHER", "Other");

    private AssetType(string value, string name)
        : base(name, value)
    {
    }

    public bool IsCrypto() => this == Crypto;
}