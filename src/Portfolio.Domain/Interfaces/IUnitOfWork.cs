using System;
using System.Threading;
using System.Threading.Tasks;

namespace Portfolio.Domain.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    ITransactionRepository Transactions { get; }
    IAssetRepository Assets { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
