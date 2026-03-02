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
        await context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Transaction transaction)
    {
        context.Transactions.Update(transaction);
        await context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var transaction = await context.Transactions.FindAsync(id);
        if (transaction != null)
        {
            context.Transactions.Remove(transaction);
            await context.SaveChangesAsync();
        }
    }
}
