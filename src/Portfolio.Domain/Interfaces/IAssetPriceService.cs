using Portfolio.Domain.Enums;

namespace Portfolio.Domain.Interfaces;

public interface IAssetPriceService
{
    /// <summary>
    /// Gets the price of multiple assets in the specified base currency.
    /// handles smart routing between Crypto and Fiat providers.
    /// </summary>
    /// <param name="assetIds">List of asset IDs (e.g., "bitcoin", "ethereum", "usd", "eur")</param>
    /// <param name="baseCurrency">The currency to price against (e.g., USD, EUR)</param>
    /// <returns>Dictionary of AssetId -> Price</returns>
    Task<Dictionary<string, decimal>> GetCurrentPricesAsync(IEnumerable<string> assetIds, FiatCurrency baseCurrency);
}
