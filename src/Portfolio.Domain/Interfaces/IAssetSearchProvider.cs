using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Interfaces;

public interface IAssetSearchProvider
{
    Task<IEnumerable<Asset>> SearchAssetsAsync(AssetType type, string query);
}
