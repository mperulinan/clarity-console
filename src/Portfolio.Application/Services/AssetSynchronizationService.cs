using Microsoft.Extensions.Logging;
using Portfolio.Application.Interfaces;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Interfaces;

namespace Portfolio.Application.Services;

public class AssetSynchronizationService(
    IAssetRepository assetRepository,
    ILogger<AssetSynchronizationService> logger) : IAssetSynchronizationService
{
    public async Task<int> SynchronizeCatalogAsync(IEnumerable<Asset> externalAssets)
    {
        var externalList = externalAssets.ToList();
        if (externalList.Count == 0)
        {
            logger.LogDebug("SynchronizeCatalogAsync called with empty list. Skipping.");
            return 0;
        }

        logger.LogInformation("Starting catalog synchronization for {Count} external assets...", externalList.Count);
        
        var externalIds = externalList.Select(a => a.ExternalId!).ToList();
        
        var existingAssets = await assetRepository.GetByExternalIdsAsync(externalIds);
        var existingDict = existingAssets.ToDictionary(a => a.ExternalId!, StringComparer.OrdinalIgnoreCase);

        int updatedOrInserted = 0;

        foreach (var incoming in externalList)
        {
            if (existingDict.TryGetValue(incoming.ExternalId!, out var existing))
            {
                if (existing.Symbol != incoming.Symbol || existing.Name != incoming.Name || existing.ImageUrl != incoming.ImageUrl)
                {
                    existing.UpdateMetadata(incoming.Symbol, incoming.Name, incoming.ImageUrl);
                    await assetRepository.UpdateAsync(existing);
                    updatedOrInserted++;
                }
            }
            else
            {
                await assetRepository.AddAsync(incoming);
                updatedOrInserted++;
            }
        }
        
        logger.LogInformation("Catalog synchronization complete. {UpdatedOrInserted} assets were updated or inserted.", updatedOrInserted);
        return updatedOrInserted;
    }
}
