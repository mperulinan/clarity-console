using Portfolio.Domain.Enums;

namespace Portfolio.Domain.Interfaces;

public interface IAssetPriceProvider
{
    bool Supports(AssetType type);
    Task<Dictionary<string, decimal>> GetPricesAsync(IEnumerable<string> externalIds, FiatCurrency currency, AssetType type);
}
