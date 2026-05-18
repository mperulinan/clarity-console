using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Interfaces;

namespace Portfolio.Infrastructure.ExternalServices;

/// <summary>
/// Caching decorator that wraps a single concrete <see cref="IAssetSearchProvider"/>.
/// Caches search results to avoid hitting provider API rate limits for identical queries.
/// </summary>
public class AssetSearchCacheService(
    IAssetSearchProvider innerProvider,
    IMemoryCache memoryCache,
    ILogger<AssetSearchCacheService> logger) : IAssetSearchProvider
{
    public async Task<IEnumerable<SearchAssetResult>> SearchAssetsAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        // Include the inner provider type in the key so different providers don't collide
        string cacheKey = $"AssetSearch_{innerProvider.GetType().Name}_{query.Trim().ToLowerInvariant()}";

        if (memoryCache.TryGetValue(cacheKey, out IEnumerable<SearchAssetResult>? cachedResults))
        {
            logger.LogDebug("Cache hit for search query '{Query}' on {Provider}", query, innerProvider.GetType().Name);
            return cachedResults ?? [];
        }

        logger.LogInformation("Cache miss for search query '{Query}' on {Provider}. Fetching...", query, innerProvider.GetType().Name);
        
        var freshResults = (await innerProvider.SearchAssetsAsync(query)).ToList();

        // Cache search results for 24 hours if we got results.
        // If empty, cache for only 5 minutes in case it was a transient rate limit or network error.
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = freshResults.Count > 0 
                ? TimeSpan.FromHours(24) 
                : TimeSpan.FromMinutes(5)
        };

        memoryCache.Set(cacheKey, freshResults, cacheOptions);

        return freshResults;
    }
}
