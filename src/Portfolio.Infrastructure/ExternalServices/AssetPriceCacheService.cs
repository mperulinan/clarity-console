using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;

namespace Portfolio.Infrastructure.ExternalServices;

/// <summary>
/// Caching decorator that wraps a single concrete <see cref="IAssetPriceProvider"/>.
/// Provider routing is handled upstream via Keyed DI — this class only caches.
/// </summary>
public class AssetPriceCacheService(
    IAssetPriceProvider innerProvider,
    IMemoryCache memoryCache,
    ILogger<AssetPriceCacheService> logger) : IAssetPriceProvider
{
    private const string CacheKeyPrefix = "AssetPrice";

    public async Task<Dictionary<string, decimal>> GetPricesAsync(
        IEnumerable<string> externalIds, FiatCurrency currency)
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
            logger.LogInformation(
                "Cache miss for {Count} asset(s). Fetching from {Provider}...",
                missingIds.Count, innerProvider.GetType().Name);

            var freshPrices = await innerProvider.GetPricesAsync(missingIds, currency);

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

    public async Task<decimal?> GetHistoricalPriceAsync(string externalId, FiatCurrency currency, DateTime date)
    {
        string cacheKey = $"{CacheKeyPrefix}_{externalId}_{currency.Value}_{date:yyyyMMdd}";

        if (memoryCache.TryGetValue(cacheKey, out decimal cachedPrice))
        {
            logger.LogDebug("Cache hit for historical asset {AssetId} ({Currency}) at {Date}", externalId, currency.Value, date.ToString("yyyyMMdd"));
            return cachedPrice;
        }

        var price = await innerProvider.GetHistoricalPriceAsync(externalId, currency, date);
        if (price.HasValue)
        {
            bool isToday = date.Date == DateTime.UtcNow.Date;

            var cacheOptions = new MemoryCacheEntryOptions();

            if (isToday)
            {
                cacheOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15);
            }
            else
            {
                cacheOptions.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30);
                cacheOptions.Priority = CacheItemPriority.High;
            }

            memoryCache.Set(cacheKey, price.Value, cacheOptions);
        }
        return price;
    }
}
