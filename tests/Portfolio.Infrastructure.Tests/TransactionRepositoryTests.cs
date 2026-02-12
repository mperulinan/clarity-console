using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Infrastructure.Persistence;
using Portfolio.Infrastructure.Persistence.Repositories;
using Xunit;

namespace Portfolio.Infrastructure.Tests;

public class TransactionRepositoryTests
{
    private readonly DbContextOptions<PortfolioContext> _options;

    public TransactionRepositoryTests()
    {
        _options = new DbContextOptionsBuilder<PortfolioContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique DB per test class/run
            .Options;
    }

    [Fact]
    public async Task AddAsync_ShouldAddTransactionToDatabase()
    {
        using (PortfolioContext context = new(_options))
        {
            TransactionRepository repository = new(context);
            Transaction transaction = new(DateTime.UtcNow, TransactionTypeEnum.Swap, "USD", "BTC", 100, 1, 1, null, 0, null, null, null, null, null);

            await repository.AddAsync(transaction);
        }

        using (PortfolioContext assertContext = new(_options))
        {
            Assert.Equal(1, await assertContext.Transactions.CountAsync());
            var saved = await assertContext.Transactions.FirstAsync();
            Assert.Equal("BTC", saved.ToAssetId);
        }
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnTransaction_WhenExists()
    {
        using (PortfolioContext context = new(_options))
        {
            Transaction tx = new(DateTime.UtcNow, TransactionTypeEnum.Swap, "USD", "BTC", 100, 1, 1, null, 0, null, null, null, null, null);
            context.Transactions.Add(tx);
            await context.SaveChangesAsync();
        }

        using (PortfolioContext context = new(_options))
        {
            TransactionRepository repository = new(context);

            var tx = await context.Transactions.FirstAsync();
            Transaction? result = await repository.GetByIdAsync(tx.Id);

            Assert.NotNull(result);
            Assert.Equal("BTC", result.ToAssetId);
        }
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateTransaction()
    {
        int id;
        using (PortfolioContext context = new(_options))
        {
            Transaction tx = new(DateTime.UtcNow, TransactionTypeEnum.Swap, "USD", "ETH", 100, 1, 1, null, 0, null, null, null, null, null);
            context.Transactions.Add(tx);
            await context.SaveChangesAsync();
            id = tx.Id;
        }

        using (PortfolioContext context = new(_options))
        {
            TransactionRepository repository = new(context);
            var tx = await repository.GetByIdAsync(id);
            Assert.NotNull(tx);
            
            tx.UpdateExchangeRates(0.9m);
            
            await repository.UpdateAsync(tx);
        }

        using (PortfolioContext context = new(_options))
        {
            var savedTx = await context.Transactions.FindAsync(id);
            Assert.NotNull(savedTx);
            Assert.Equal(0.9m, savedTx.UsdEurExchangeRate);
        }
    }
}
