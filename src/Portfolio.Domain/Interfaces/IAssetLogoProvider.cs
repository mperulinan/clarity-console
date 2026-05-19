using Portfolio.Domain.Enums;

namespace Portfolio.Domain.Interfaces;

public interface IAssetLogoProvider
{
    bool Supports(AssetType type);
    Task<string?> GetLogoUrlAsync(string symbol);
}
