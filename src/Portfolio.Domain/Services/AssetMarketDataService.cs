using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Services;

public class AssetMarketDataService(
    IAssetRepository assetRepository,
    IAssetPriceProvider assetPriceProvider,
    IExchangeRateProvider exchangeRateProvider) : IAssetMarketDataService
{
    public async Task<Dictionary<Guid, AssetMarketData>> GetMarketDataAsync(IEnumerable<Guid> assetIds, FiatCurrency baseCurrency)
    {
        Dictionary<Guid, AssetMarketData> marketData = [];

        // 1. Retrieve and Classify in one go using Lookup
        var assets = await assetRepository.GetByIdsAsync(assetIds.Distinct());
        var assetsByType = assets.ToLookup(a => a.Type);

        // 2. Resolve "OTHER" Assets (Manual/No Price)
        foreach (var asset in assetsByType[AssetType.Other])
        {
            marketData[asset.Id] = new AssetMarketData(asset.Symbol, asset.Name, 0m, asset.ImageUrl);
        }

        // 3. Resolve Fiat Prices
        foreach (var asset in assetsByType[AssetType.Fiat])
        {
            var price = await GetFiatPriceAsync(asset, baseCurrency);
            marketData[asset.Id] = new AssetMarketData(asset.Symbol, asset.Name, price, asset.ImageUrl);
        }

        // 4. Resolve Crypto, Stock and Index prices (each routed to the correct provider via the cache service)
        var pricedTypes = new[] { AssetType.Crypto, AssetType.Stock, AssetType.Index };
        foreach (var assetType in pricedTypes)
        {
            var typedAssets = assetsByType[assetType].ToList();
            if (typedAssets.Count == 0) continue;

            if (!assetPriceProvider.Supports(assetType))
            {
                foreach (var asset in typedAssets)
                    marketData[asset.Id] = new AssetMarketData(asset.Symbol, asset.Name, 0m, asset.ImageUrl);
                continue;
            }

            var externalIds = typedAssets
                .Where(a => !string.IsNullOrWhiteSpace(a.ExternalId))
                .Select(a => a.ExternalId!);

            var prices = await assetPriceProvider.GetPricesAsync(externalIds, baseCurrency, assetType);

            foreach (var asset in typedAssets)
            {
                if (asset.ExternalId != null && prices.TryGetValue(asset.ExternalId, out var price))
                    marketData[asset.Id] = new AssetMarketData(asset.Symbol, asset.Name, price, asset.ImageUrl);
                else
                    marketData[asset.Id] = new AssetMarketData(asset.Symbol, asset.Name, 0m, asset.ImageUrl);
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
