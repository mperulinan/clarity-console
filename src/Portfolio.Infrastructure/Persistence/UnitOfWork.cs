using System.Threading;
using System.Threading.Tasks;
using Portfolio.Domain.Interfaces;
using Portfolio.Infrastructure.Persistence.Repositories;

namespace Portfolio.Infrastructure.Persistence;

public sealed class UnitOfWork(
    PortfolioContext context,
    ITransactionRepository transactions,
    IAssetRepository assets) : IUnitOfWork
{
    public ITransactionRepository Transactions { get; } = transactions;
    public IAssetRepository Assets { get; } = assets;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await context.SaveChangesAsync(cancellationToken);

    public async ValueTask DisposeAsync() => await context.DisposeAsync();
}
