using Ardalis.SmartEnum;

namespace Portfolio.Domain.Enums;

public class AssetType : SmartEnum<AssetType, string>
{
    public static readonly AssetType Crypto = new("CRYPTO", "Crypto", requiresMarketData: true);
    public static readonly AssetType Fiat = new("FIAT", "Fiat", requiresMarketData: false);
    public static readonly AssetType Stock = new("STOCK", "Stock", requiresMarketData: true);
    public static readonly AssetType Etf = new("ETF", "ETF", requiresMarketData: true);
    public static readonly AssetType Other = new("OTHER", "Other", requiresMarketData: false);

    public bool RequiresMarketData { get; }

    private AssetType(string value, string name, bool requiresMarketData)
        : base(name, value)
    {
        RequiresMarketData = requiresMarketData;
    }

    public bool IsCrypto() => this == Crypto;
}