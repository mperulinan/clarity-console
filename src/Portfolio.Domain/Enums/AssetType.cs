using Ardalis.SmartEnum;

namespace Portfolio.Domain.Enums;

public class AssetType : SmartEnum<AssetType, string>
{
    public static readonly AssetType Crypto = new("CRYPTO", "Crypto", requiresMarketData: true, canBeSearchedExternally: true);
    public static readonly AssetType Fiat = new("FIAT", "Fiat", requiresMarketData: false, canBeSearchedExternally: false);
    public static readonly AssetType Stock = new("STOCK", "Stock", requiresMarketData: true, canBeSearchedExternally: true);
    public static readonly AssetType Index = new("INDEX", "Index", requiresMarketData: true, canBeSearchedExternally: true);
    public static readonly AssetType Other = new("OTHER", "Other", requiresMarketData: false, canBeSearchedExternally: false);

    public bool RequiresMarketData { get; }
    public bool CanBeSearchedExternally { get; }

    private AssetType(string value, string name, bool requiresMarketData, bool canBeSearchedExternally)
        : base(name, value)
    {
        RequiresMarketData = requiresMarketData;
        CanBeSearchedExternally = canBeSearchedExternally;
    }

    public bool IsCrypto() => this == Crypto;
}