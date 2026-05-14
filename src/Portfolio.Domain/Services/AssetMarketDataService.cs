using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Services;

public class AssetMarketDataService(
    IAssetRepository assetRepository,
    IAssetPriceProviderFactory priceProviderFactory,
    IExchangeRateProvider exchangeRateProvider) : IAssetMarketDataService
{
    public async Task<Dictionary<Guid, AssetMarketData>> GetMarketDataAsync(IEnumerable<Guid> assetIds, FiatCurrency baseCurrency)
    {
        Dictionary<Guid, AssetMarketData> marketData = [];

        var assets = await assetRepository.GetByIdsAsync(assetIds.Distinct());
        var assetsByType = assets.ToLookup(a => a.Type);

        foreach (var group in assetsByType)
        {
            var assetType = group.Key;
            var typedAssets = group.ToList();

            if (assetType == AssetType.Other)
            {
                foreach (var asset in typedAssets)
                    marketData[asset.Id] = new AssetMarketData(asset.Symbol, asset.Name, 0m, asset.ImageUrl);

                continue;
            }

            if (assetType == AssetType.Fiat)
            {
                foreach (var asset in typedAssets)
                {
                    var price = await GetFiatPriceAsync(asset, baseCurrency);
                    marketData[asset.Id] = new AssetMarketData(asset.Symbol, asset.Name, price, asset.ImageUrl);
                }

                continue;
            }

            if (assetType.RequiresMarketData)
            {
                var provider = priceProviderFactory.GetProvider(assetType);

                if (provider == null)
                {
                    foreach (var asset in typedAssets)
                        marketData[asset.Id] = new AssetMarketData(asset.Symbol, asset.Name, 0m, asset.ImageUrl);

                    continue;
                }

                var externalIds = typedAssets
                    .Where(a => !string.IsNullOrWhiteSpace(a.ExternalId))
                    .Select(a => a.ExternalId!);

                var prices = await provider.GetPricesAsync(externalIds, baseCurrency);

                foreach (var asset in typedAssets)
                {
                    var price = asset.ExternalId != null && prices.TryGetValue(asset.ExternalId, out var p) ? p : 0m;
                    marketData[asset.Id] = new AssetMarketData(asset.Symbol, asset.Name, price, asset.ImageUrl);
                }
            }
        }

        return marketData;
    }

    private async Task<decimal> GetFiatPriceAsync(Asset fiatAsset, FiatCurrency baseCurrency)
    {
        FiatCurrency assetCurrency = FiatCurrency.Parse(fiatAsset.Symbol);
        if (assetCurrency == baseCurrency) return 1.0m;
        return await exchangeRateProvider.GetExchangeRateAsync(assetCurrency, baseCurrency);
    }
}
