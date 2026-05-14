using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;

/// <summary>
/// Resolves the correct <see cref="IAssetPriceProvider"/> for a given asset type.
/// Keeps the Domain layer free from DI-container references.
/// </summary>
public interface IAssetPriceProviderFactory
{
    IAssetPriceProvider? GetProvider(AssetType type);
}
