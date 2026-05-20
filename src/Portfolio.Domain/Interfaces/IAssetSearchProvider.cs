namespace Portfolio.Domain.Interfaces;

public interface IAssetSearchProvider
{
    Task<IEnumerable<SearchAssetResult>> SearchAssetsAsync(string query);
}
