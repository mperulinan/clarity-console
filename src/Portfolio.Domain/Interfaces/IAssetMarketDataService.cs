using Portfolio.Domain.Enums;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Interfaces;

public interface IAssetMarketDataService
{
    Task<Dictionary<string, AssetMarketData>> GetMarketDataAsync(IEnumerable<string> assetIds, FiatCurrency baseCurrency);
}
