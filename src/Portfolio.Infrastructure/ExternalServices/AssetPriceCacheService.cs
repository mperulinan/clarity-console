using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;

namespace Portfolio.Infrastructure.ExternalServices;

/// <summary>
/// Caching decorator that wraps multiple concrete price providers
/// and routes each request to whichever provider supports the given asset type.
/// </summary>
public class AssetPriceCacheService(
    IEnumerable<IAssetPriceProvider> innerProviders,
    IMemoryCache memoryCache,
    ILogger<AssetPriceCacheService> logger) : IAssetPriceProvider
{
    private const string CacheKeyPrefix = "AssetPrice";

    /// <summary>Returns true when at least one inner provider supports the type.</summary>
    public bool Supports(AssetType type) => innerProviders.Any(p => p.Supports(type));

    public async Task<Dictionary<string, decimal>> GetPricesAsync(
        IEnumerable<string> externalIds, FiatCurrency currency, AssetType type)
    {
        var idsArray = externalIds.Distinct().ToArray();
        var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        var missingIds = new List<string>();

        foreach (var id in idsArray)
        {
            string cacheKey = $"{CacheKeyPrefix}_{id}_{currency.Value}";
            if (memoryCache.TryGetValue(cacheKey, out decimal cachedPrice))
            {
                logger.LogDebug("Cache hit for asset {AssetId} ({Currency})", id, currency.Value);
                result[id] = cachedPrice;
            }
            else
            {
                missingIds.Add(id);
            }
        }

        if (missingIds.Count > 0)
        {
            var provider = innerProviders.FirstOrDefault(p => p.Supports(type));
            if (provider == null)
            {
                logger.LogWarning("No price provider supports asset type {Type}.", type.Value);
                return result;
            }

            logger.LogInformation(
                "Cache miss for {Count} assets ({Type}). Fetching from {Provider}...",
                missingIds.Count, type.Value, provider.GetType().Name);

            var freshPrices = await provider.GetPricesAsync(missingIds, currency, type);

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            };

            foreach (var kvp in freshPrices)
            {
                string cacheKey = $"{CacheKeyPrefix}_{kvp.Key}_{currency.Value}";
                memoryCache.Set(cacheKey, kvp.Value, cacheOptions);
                result[kvp.Key] = kvp.Value;
            }
        }

        return result;
    }

    // Explicit interface implementation to satisfy the contract.
    // The type-aware overload is the real entry point; this overload is not used.
    Task<Dictionary<string, decimal>> IAssetPriceProvider.GetPricesAsync(
        IEnumerable<string> externalIds, FiatCurrency currency, AssetType type)
        => GetPricesAsync(externalIds, currency, type);
}
