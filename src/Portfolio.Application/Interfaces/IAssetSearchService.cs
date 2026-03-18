using Portfolio.Application.DTOs;

namespace Portfolio.Application.Interfaces;

public interface IAssetSearchService
{
    Task<IEnumerable<AssetDto>> SearchAsync(string query, string? type = null);
}
