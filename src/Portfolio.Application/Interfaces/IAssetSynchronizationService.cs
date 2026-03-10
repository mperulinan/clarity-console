using Portfolio.Domain.Entities;

namespace Portfolio.Application.Interfaces;

public interface IAssetSynchronizationService
{
    Task<int> SynchronizeCatalogAsync(IEnumerable<Asset> externalAssets);
}
