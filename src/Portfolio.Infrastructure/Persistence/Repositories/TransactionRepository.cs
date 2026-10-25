using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Interfaces;

namespace Portfolio.Infrastructure.Persistence.Repositories;

public class TransactionRepository(PortfolioContext context) : ITransactionRepository
{
    public async Task<IEnumerable<Transaction>> GetAllAsync()
    {
        return await context.Transactions
            .Include(t => t.FromAsset)
            .Include(t => t.ToAsset)
            .Include(t => t.FeeAsset)
            .ToListAsync();
    }

    public async Task<Transaction?> GetByIdAsync(int id)
    {
        return await context.Transactions
            .Include(t => t.FromAsset)
            .Include(t => t.ToAsset)
            .Include(t => t.FeeAsset)
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task AddAsync(Transaction transaction)
    {
        await context.Transactions.AddAsync(transaction);
    }

    public async Task UpdateAsync(Transaction transaction)
    {
        context.Transactions.Update(transaction);
        await Task.CompletedTask; // Keep async signature without SaveChanges
    }

    public async Task DeleteAsync(int id)
    {
        var transaction = await context.Transactions.FindAsync(id);
        if (transaction != null)
        {
            context.Transactions.Remove(transaction);
        }
    }

    public async Task<bool> ExistsByBrokerTransactionIdAsync(string brokerTransactionId, CancellationToken cancellationToken = default)
    {
        return await context.Transactions.AnyAsync(t => t.BrokerTransactionId == brokerTransactionId, cancellationToken);
    }

    public async Task<IReadOnlySet<string>> GetExistingBrokerTransactionIdsAsync(IEnumerable<string> brokerTransactionIds, CancellationToken cancellationToken = default)
    {
        var idsList = brokerTransactionIds.ToList();
        if (idsList.Count == 0)
        {
            return new HashSet<string>();
        }

        var existing = await context.Transactions
            .Where(t => t.BrokerTransactionId != null && idsList.Contains(t.BrokerTransactionId))
            .Select(t => t.BrokerTransactionId!)
            .ToListAsync(cancellationToken);

        return existing.ToHashSet();
    }
}
