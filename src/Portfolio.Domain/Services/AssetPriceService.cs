using Portfolio.Domain.Enums;
using Portfolio.Domain.Constants;
using Portfolio.Domain.Interfaces;

namespace Portfolio.Domain.Services;

public class AssetPriceService(
    ICryptoPriceProvider cryptoPriceProvider,
    IExchangeRateProvider exchangeRateProvider) : IAssetPriceService
{
    public async Task<Dictionary<string, decimal>> GetCurrentPricesAsync(IEnumerable<string> assetIds, FiatCurrency baseCurrency)
    {
        Dictionary<string, decimal> prices = [];
        List<string> cryptoIds = [];
        List<string> fiatIds = [];

        // 1. Classify Assets
        foreach (string id in assetIds)
        {
            if (IsFiat(id))
            {
                fiatIds.Add(id);
            }
            else
            {
                cryptoIds.Add(id);
            }
        }

        // 2. Resolve Crypto Prices
        if (cryptoIds.Count > 0)
        {
            var cryptoPrices = await cryptoPriceProvider.GetCurrentCryptoPricesAsync(cryptoIds, baseCurrency);
            foreach (var kvp in cryptoPrices)
            {
                prices[kvp.Key] = kvp.Value;
            }
        }

        // 3. Resolve Fiat Prices
        if (fiatIds.Count > 0)
        {
            foreach (var fiatId in fiatIds)
            {
                var price = await GetFiatPriceAsync(fiatId, baseCurrency);
                prices[fiatId] = price;
            }
        }

        return prices;
    }

    private static bool IsFiat(string assetId) => assetId.ToUpperInvariant() switch
    {
        CurrencyConstants.Usd or CurrencyConstants.Eur => true,
        _ => false
    };

    private async Task<decimal> GetFiatPriceAsync(string fiatAssetId, FiatCurrency baseCurrency)
    {
        // Parse Asset ID to Enum
        var assetCurrency = ParseCurrency(fiatAssetId);

        // Case 1: Same Currency (e.g. Price of USD in USD)
        if (assetCurrency == baseCurrency)
        {
            return 1.0m;
        }

        // Case 2: Different Currency (e.g. Price of EUR in USD)
        // Rate: EUR -> USD
        return await exchangeRateProvider.GetExchangeRateAsync(assetCurrency, baseCurrency);
    }

    private static FiatCurrency ParseCurrency(string assetId)
    {
        return assetId.ToLowerInvariant() switch
        {
            CurrencyConstants.Usd => FiatCurrency.USD,
            CurrencyConstants.Eur => FiatCurrency.EUR,
            _ => throw new ArgumentException($"Unknown fiat currency: {assetId}")
        };
    }
}
