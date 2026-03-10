using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Interfaces;

namespace Portfolio.Infrastructure.Persistence.Repositories;

public class AssetRepository(PortfolioContext context) : IAssetRepository
{
    public async Task<Asset?> GetByIdAsync(Guid id)
    {
        return await context.Assets.FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<IEnumerable<Asset>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        return await context.Assets.Where(a => ids.Contains(a.Id)).ToListAsync();
    }

    public async Task<IEnumerable<Asset>> GetByExternalIdsAsync(IEnumerable<string> externalIds)
    {
        return await context.Assets.Where(a => a.ExternalId != null && externalIds.Contains(a.ExternalId)).ToListAsync();
    }

    public async Task<IEnumerable<Asset>> GetAllAsync()
    {
        return await context.Assets.ToListAsync();
    }

    public async Task AddAsync(Asset asset)
    {
        await context.Assets.AddAsync(asset);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Asset asset)
    {
        context.Assets.Update(asset);
        await context.SaveChangesAsync();
    }
}
