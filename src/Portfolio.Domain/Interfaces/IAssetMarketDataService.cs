using Portfolio.Domain.Enums;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Interfaces;

public interface IAssetMarketDataService
{
    Task<Dictionary<Guid, AssetMarketData>> GetMarketDataAsync(IEnumerable<Guid> assetIds, FiatCurrency baseCurrency);
}
