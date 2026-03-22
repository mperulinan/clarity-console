using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;

namespace Portfolio.Domain.Interfaces;

public interface IAssetCatalogProvider
{
    Task<IEnumerable<Asset>> GetTopAssetsAsync(AssetType type, int count = 250);
    Task<IEnumerable<Asset>> GetAssetsByExternalIdsAsync(AssetType type, IEnumerable<string> externalIds);
}
