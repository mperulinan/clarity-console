using Portfolio.Application.Models;

namespace Portfolio.Application.Interfaces;

public interface IAssetSearchProvider
{
    Task<IEnumerable<SearchAssetResult>> SearchAssetsAsync(string query);
}
