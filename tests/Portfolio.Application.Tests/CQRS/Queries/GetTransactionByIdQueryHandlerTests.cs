using Portfolio.Application.CQRS.Queries;
using Portfolio.Application.Tests.Fakes;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Xunit;

namespace Portfolio.Application.Tests.CQRS.Queries;

public class GetTransactionByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_WithExistingId_ShouldReturnTransactionDto()
    {
        // Arrange
        var uow = new FakeUnitOfWork();
        
        var usd = new Asset("USD", "US Dollar", null, null, AssetType.Fiat);
        var btc = new Asset("BTC", "Bitcoin", null, null, AssetType.Crypto);
        await uow.Assets.AddAsync(usd);
        await uow.Assets.AddAsync(btc);

        var tx = new Transaction(DateTime.UtcNow, TransactionType.Swap, usd.Id, btc.Id, 1000, 0.1m, 10000, null, 0, null, null, null, null, FiatCurrency.USD, null, null)
        {
            FromAsset = usd,
            ToAsset = btc
        };
        await uow.Transactions.AddAsync(tx);

        var handler = new GetTransactionByIdQueryHandler(uow);
        var query = new GetTransactionByIdQuery(tx.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tx.Id, result.Id);
        Assert.Equal(1000, result.AmountSpent);
        Assert.Equal(0.1m, result.AmountReceived);
        Assert.Equal(usd.Id, result.FromAsset?.Id);
        Assert.Equal(btc.Id, result.ToAsset?.Id);
    }

    [Fact]
    public async Task Handle_WithNonExistingId_ShouldReturnNull()
    {
        // Arrange
        var uow = new FakeUnitOfWork();
        var handler = new GetTransactionByIdQueryHandler(uow);
        var query = new GetTransactionByIdQuery(999); // Non-existing ID

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }
}
