using System.Threading;
using System.Threading.Tasks;
using Portfolio.Domain.Interfaces;
using Portfolio.Infrastructure.Persistence.Repositories;

namespace Portfolio.Infrastructure.Persistence;

public sealed class UnitOfWork(PortfolioContext context) : IUnitOfWork
{
    public ITransactionRepository Transactions { get; } = new TransactionRepository(context);
    public IAssetRepository Assets { get; } = new AssetRepository(context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await context.SaveChangesAsync(cancellationToken);

    public async ValueTask DisposeAsync() => await context.DisposeAsync();
}
