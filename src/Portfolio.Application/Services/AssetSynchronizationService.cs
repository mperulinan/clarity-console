using Portfolio.Application.Interfaces;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Interfaces;

namespace Portfolio.Application.Services;

public class AssetSynchronizationService(IAssetRepository assetRepository) : IAssetSynchronizationService
{
    public async Task<int> SynchronizeCatalogAsync(IEnumerable<Asset> externalAssets)
    {
        var externalList = externalAssets.ToList();
        if (externalList.Count == 0)
        {
            return 0;
        }

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
        
        return updatedOrInserted;
    }
}
