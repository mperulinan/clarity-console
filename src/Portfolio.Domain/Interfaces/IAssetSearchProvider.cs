using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;

namespace Portfolio.Domain.Interfaces;

public interface IAssetSearchProvider
{
    Task<IEnumerable<SearchAssetResult>> SearchAssetsAsync(AssetType type, string query);
}
