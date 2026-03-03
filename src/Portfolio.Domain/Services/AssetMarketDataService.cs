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
        List<Asset> cryptoAssets = [];
        List<Asset> fiatAssets = [];

        // 1. Classify Assets by retrieving them from repository in a single query
        var assets = await assetRepository.GetByIdsAsync(assetIds.Distinct());
        
        foreach (var asset in assets)
        {
            if (asset.Type == AssetType.Fiat)
            {
                fiatAssets.Add(asset);
            }
            else // Currently everything else is treated as crypto
            {
                cryptoAssets.Add(asset);
            }
        }

        // 2. Resolve Crypto Market Data
        if (cryptoAssets.Count > 0)
        {
            var externalIds = cryptoAssets
                .Where(a => !string.IsNullOrWhiteSpace(a.ExternalId))
                .Select(a => a.ExternalId!);

            var cryptoData = await cryptoMarketDataProvider.GetCryptoMarketDataAsync(externalIds, baseCurrency);
            
            foreach (var asset in cryptoAssets)
            {
                if (cryptoData.TryGetValue(asset.ExternalId!, out var data))
                {
                    marketData[asset.Id] = data;
                }
            }
        }

        // 3. Resolve Fiat Prices
        if (fiatAssets.Count > 0)
        {
            foreach (var asset in fiatAssets)
            {
                var price = await GetFiatPriceAsync(asset, baseCurrency);
                marketData[asset.Id] = new AssetMarketData(asset.Symbol, asset.Name, price);
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
