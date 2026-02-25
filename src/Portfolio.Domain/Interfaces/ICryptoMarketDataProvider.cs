using Portfolio.Domain.Enums;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Interfaces;

public interface ICryptoMarketDataProvider
{
    Task<Dictionary<string, AssetMarketData>> GetCryptoMarketDataAsync(IEnumerable<string> assetIds, FiatCurrency priceCurrency);
}
