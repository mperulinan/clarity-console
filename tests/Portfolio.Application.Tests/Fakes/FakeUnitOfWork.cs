using Portfolio.Domain.Entities;
using Portfolio.Domain.Interfaces;

namespace Portfolio.Application.Tests.Fakes;

// ---------------------------------------------------------------------------
// Fake Repositories — in-memory, no EF Core needed
// ---------------------------------------------------------------------------

public class FakeTransactionRepository : ITransactionRepository
{
    private readonly List<Transaction> _transactions = [];

    public Task<IEnumerable<Transaction>> GetAllAsync() =>
        Task.FromResult<IEnumerable<Transaction>>([.. _transactions]);

    public Task<Transaction?> GetByIdAsync(int id) =>
        Task.FromResult(_transactions.FirstOrDefault(t => t.Id == id));

    public Task AddAsync(Transaction transaction)
    {
        _transactions.Add(transaction);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Transaction transaction) =>
        Task.CompletedTask; // entity already mutated in-place by reference

    public Task DeleteAsync(int id)
    {
        _transactions.RemoveAll(t => t.Id == id);
        return Task.CompletedTask;
    }
}

public class FakeAssetRepository : IAssetRepository
{
    private readonly List<Asset> _assets = [];

    public Task<Asset?> GetByIdAsync(Guid id) =>
        Task.FromResult(_assets.FirstOrDefault(a => a.Id == id));

    public Task<IEnumerable<Asset>> GetByIdsAsync(IEnumerable<Guid> ids)
    {
        var idSet = ids.ToHashSet();
        return Task.FromResult<IEnumerable<Asset>>(_assets.Where(a => idSet.Contains(a.Id)).ToList());
    }

    public Task<IEnumerable<Asset>> GetByExternalIdsAsync(IEnumerable<string> externalIds)
    {
        var idSet = externalIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return Task.FromResult<IEnumerable<Asset>>(_assets.Where(a => a.ExternalId != null && idSet.Contains(a.ExternalId)).ToList());
    }

    public Task<IEnumerable<Asset>> GetAllAsync() =>
        Task.FromResult<IEnumerable<Asset>>([.. _assets]);

    public Task AddAsync(Asset asset) { _assets.Add(asset); return Task.CompletedTask; }
    public Task UpdateAsync(Asset asset) => Task.CompletedTask;
}

// ---------------------------------------------------------------------------
// Fake Unit of Work — tracks SaveChangesAsync call count
// ---------------------------------------------------------------------------

public class FakeUnitOfWork : IUnitOfWork
{
    public FakeTransactionRepository FakeTransactions { get; } = new();
    public FakeAssetRepository FakeAssets { get; } = new();

    public ITransactionRepository Transactions => FakeTransactions;
    public IAssetRepository Assets => FakeAssets;

    public int SaveChangesCallCount { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveChangesCallCount++;
        return Task.FromResult(0);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
