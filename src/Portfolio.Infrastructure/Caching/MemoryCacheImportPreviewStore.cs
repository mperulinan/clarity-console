using Microsoft.Extensions.Caching.Memory;
using Portfolio.Application.Interfaces;
using Portfolio.Application.Models;

namespace Portfolio.Infrastructure.Caching;

public class MemoryCacheImportPreviewStore(IMemoryCache memoryCache) : IImportPreviewStore
{
    private static readonly TimeSpan DefaultSlidingExpiration = TimeSpan.FromMinutes(20);
    private static readonly TimeSpan DefaultAbsoluteExpiration = TimeSpan.FromHours(1);

    public ImportPreviewSession? Get(string previewToken)
    {
        return memoryCache.TryGetValue<ImportPreviewSession>(previewToken, out var session)
            ? session
            : null;
    }

    public void Set(string previewToken, ImportPreviewSession session, TimeSpan? slidingExpiration = null)
    {
        var options = new MemoryCacheEntryOptions
        {
            SlidingExpiration = slidingExpiration ?? DefaultSlidingExpiration,
            AbsoluteExpirationRelativeToNow = DefaultAbsoluteExpiration
        };

        memoryCache.Set(previewToken, session, options);
    }

    public void Remove(string previewToken)
    {
        memoryCache.Remove(previewToken);
    }
}
