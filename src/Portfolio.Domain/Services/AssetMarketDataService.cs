using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Services;

public class AssetMarketDataService(
    IAssetRepository assetRepository,
    ICryptoMarketDataProvider cryptoMarketDataProvider,
    IExchangeRateProvider exchangeRateProvider) : IAssetMarketDataService
{
    public async Task<Dictionary<Guid, AssetMarketData>> GetMarketDataAsync(IEnumerable<Guid> assetIds, FiatCurrency baseCurrency)
    {
        Dictionary<Guid, AssetMarketData> marketData = [];

        // 1. Retrieve and Classify in one go using Lookup
        var assets = await assetRepository.GetByIdsAsync(assetIds.Distinct());
        var assetsByType = assets.ToLookup(a => a.Type);

        // 2. Resolve "OTHER" Assets (Manual/No Price)
        // We handle these first because they are "instant" (no async calls needed)
        foreach (var asset in assetsByType[AssetType.Other])
        {
            marketData[asset.Id] = new AssetMarketData(asset.Symbol, asset.Name, 0m);
        }

        // 3. Resolve Fiat Prices
        foreach (var asset in assetsByType[AssetType.Fiat])
        {
            var price = await GetFiatPriceAsync(asset, baseCurrency);
            marketData[asset.Id] = new AssetMarketData(asset.Symbol, asset.Name, price);
        }

        // 4. Resolve Crypto Market Data
        var cryptoAssets = assetsByType[AssetType.Crypto].ToList();
        if (cryptoAssets.Count > 0)
        {
            var externalIds = cryptoAssets
                .Where(a => !string.IsNullOrWhiteSpace(a.ExternalId))
                .Select(a => a.ExternalId!);

            var cryptoData = await cryptoMarketDataProvider.GetCryptoMarketDataAsync(externalIds, baseCurrency);

            foreach (var asset in cryptoAssets)
            {
                // If the provider has data, use it; otherwise, default to 0
                if (asset.ExternalId != null && cryptoData.TryGetValue(asset.ExternalId, out var data))
                {
                    marketData[asset.Id] = data;
                }
                else
                {
                    marketData[asset.Id] = new AssetMarketData(asset.Symbol, asset.Name, 0m);
                }
            }
        }

        return marketData;
    }

    private async Task<decimal> GetFiatPriceAsync(Asset fiatAsset, FiatCurrency baseCurrency)
    {
        // For Fiat assets, their Symbol corresponds to the FiatCurrency
        FiatCurrency assetCurrency = FiatCurrency.Parse(fiatAsset.Symbol);
        if (assetCurrency == baseCurrency)
        {
            return 1.0m;
        }
        return await exchangeRateProvider.GetExchangeRateAsync(assetCurrency, baseCurrency);
    }
}
