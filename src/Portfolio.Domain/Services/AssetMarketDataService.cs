using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Services;

public class AssetMarketDataService(
    ICryptoMarketDataProvider cryptoMarketDataProvider,
    IExchangeRateProvider exchangeRateProvider) : IAssetMarketDataService
{
    public async Task<Dictionary<string, AssetMarketData>> GetMarketDataAsync(IEnumerable<string> assetIds, FiatCurrency baseCurrency)
    {
        Dictionary<string, AssetMarketData> marketData = [];
        List<string> cryptoIds = [];
        List<string> fiatIds = [];

        // 1. Classify Assets
        foreach (string id in assetIds)
        {
            if (FiatCurrency.IsFiat(id))
            {
                fiatIds.Add(id);
            }
            else
            {
                cryptoIds.Add(id);
            }
        }

        // 2. Resolve Crypto Market Data (Price + Image)
        if (cryptoIds.Count > 0)
        {
            var cryptoData = await cryptoMarketDataProvider.GetCryptoMarketDataAsync(cryptoIds, baseCurrency);
            foreach (var kvp in cryptoData)
            {
                marketData[kvp.Key] = kvp.Value;
            }
        }

        // 3. Resolve Fiat Prices (Images are generally null or handled locally later)
        if (fiatIds.Count > 0)
        {
            foreach (var fiatId in fiatIds)
            {
                var price = await GetFiatPriceAsync(fiatId, baseCurrency);
                marketData[fiatId] = new AssetMarketData(price);
            }
        }

        return marketData;
    }

    private async Task<decimal> GetFiatPriceAsync(string fiatAssetId, FiatCurrency baseCurrency)
    {
        FiatCurrency assetCurrency = FiatCurrency.Parse(fiatAssetId);
        if (assetCurrency == baseCurrency)
        {
            return 1.0m;
        }
        return await exchangeRateProvider.GetExchangeRateAsync(assetCurrency, baseCurrency);
    }
}
