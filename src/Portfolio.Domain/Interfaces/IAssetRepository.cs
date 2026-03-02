using Portfolio.Domain.Entities;

namespace Portfolio.Domain.Interfaces;

public interface IAssetRepository
{
    Task<Asset?> GetByIdAsync(Guid id);
    Task<IEnumerable<Asset>> GetByIdsAsync(IEnumerable<Guid> ids);
    Task<IEnumerable<Asset>> GetAllAsync();
    Task AddAsync(Asset asset);
}
