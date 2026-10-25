using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;

namespace Portfolio.Domain.Interfaces;

public interface IAssetRepository
{
    Task<Asset?> GetByIdAsync(Guid id);
    Task<IEnumerable<Asset>> GetByIdsAsync(IEnumerable<Guid> ids);
    Task<IEnumerable<Asset>> GetByExternalIdsAsync(IEnumerable<string> externalIds);
    Task<Asset?> GetBySymbolAsync(string symbol, AssetType type, CancellationToken cancellationToken = default);
    Task<IEnumerable<Asset>> GetAllAsync();
    Task AddAsync(Asset asset);
    Task UpdateAsync(Asset asset);
}
