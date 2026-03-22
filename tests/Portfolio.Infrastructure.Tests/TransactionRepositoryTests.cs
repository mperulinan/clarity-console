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
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    private static Transaction CreateTx(
        DateTime? date = null,
        TransactionType? type = null,
        Guid? fromAssetId = null,
        Guid? toAssetId = null,
        decimal spent = 100,
        decimal received = 1,
        decimal? spotPriceUSD = 1,
        decimal? spotPriceEUR = null,
        decimal fee = 0,
        Guid? feeAssetId = null,
        decimal? feePriceUSD = null,
        decimal? feePriceEUR = null,
        decimal? usdEurRate = null,
        FiatCurrency? spotCurrency = null,
        FiatCurrency? feeCurrency = null,
        string? notes = null)
    {
        return new Transaction(
            date ?? DateTime.UtcNow,
            type ?? TransactionType.Swap,
            fromAssetId,
            toAssetId,
            spent,
            received,
            spotPriceUSD,
            spotPriceEUR,
            fee,
            feeAssetId,
            feePriceUSD,
            feePriceEUR,
            usdEurRate,
            spotCurrency ?? (spotPriceUSD.HasValue ? FiatCurrency.USD : FiatCurrency.EUR),
            feeCurrency ?? (feePriceUSD.HasValue ? FiatCurrency.USD : FiatCurrency.EUR),
            notes);
    }

    [Fact]
    public async Task AddAsync_ShouldAddTransactionToDatabase()
    {
        var usdAsset = new Asset("USD", "USD", null, null, AssetType.Fiat);
        var btcAsset = new Asset("BTC", "BTC", null, null, AssetType.Crypto);

        using (PortfolioContext context = new(_options))
        {
            context.Assets.AddRange(usdAsset, btcAsset);
            await context.SaveChangesAsync();
            
            TransactionRepository repository = new(context);
            Transaction transaction = CreateTx(fromAssetId: usdAsset.Id, toAssetId: btcAsset.Id);

            await repository.AddAsync(transaction);
        }

        using (PortfolioContext assertContext = new(_options))
        {
            Assert.Equal(1, await assertContext.Transactions.CountAsync());
            var saved = await assertContext.Transactions.Include(t => t.ToAsset).FirstAsync();
            Assert.NotNull(saved.ToAsset);
            Assert.Equal("BTC", saved.ToAsset.Symbol);
        }
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnTransaction_WhenExists()
    {
        var usdAsset = new Asset("USD", "USD", null, null, AssetType.Fiat);
        var btcAsset = new Asset("BTC", "BTC", null, null, AssetType.Crypto);

        using (PortfolioContext context = new(_options))
        {
            context.Assets.AddRange(usdAsset, btcAsset);
            Transaction tx = CreateTx(fromAssetId: usdAsset.Id, toAssetId: btcAsset.Id);
            context.Transactions.Add(tx);
            await context.SaveChangesAsync();
        }

        using (PortfolioContext context = new(_options))
        {
            TransactionRepository repository = new(context);

            var tx = await context.Transactions.FirstAsync();
            Transaction? result = await repository.GetByIdAsync(tx.Id);

            Assert.NotNull(result);
            Assert.NotNull(result.ToAsset);
            Assert.Equal("BTC", result.ToAsset.Symbol);
        }
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateTransaction()
    {
        var usdAsset = new Asset("USD", "USD", null, null, AssetType.Fiat);
        var ethAsset = new Asset("ETH", "ETH", null, null, AssetType.Crypto);
        int id;
        using (PortfolioContext context = new(_options))
        {
            context.Assets.AddRange(usdAsset, ethAsset);
            Transaction tx = CreateTx(fromAssetId: usdAsset.Id, toAssetId: ethAsset.Id);
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
