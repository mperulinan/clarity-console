using NSubstitute;
using Portfolio.Application.CQRS.Commands;
using Portfolio.Application.DTOs;
using Portfolio.Application.Tests.Fakes;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace Portfolio.Application.Tests.CQRS.Commands;

public class AddTransactionCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldStageAndPersistTransaction()
    {
        // Arrange
        var uow = new FakeUnitOfWork();
        var handler = new AddTransactionCommandHandler(uow, NullLogger<AddTransactionCommandHandler>.Instance);

        var request = new NewTransactionRequest
        {
            Date = DateTime.UtcNow,
            TransactionTypeCode = TransactionType.Swap,
            FromAssetId = Guid.NewGuid(),
            ToAssetId = Guid.NewGuid(),
            AmountSpent = 10000,
            AmountReceived = 1,
            SpotPriceUSD = 1,
            SpotPriceInputCurrency = "USD"
        };
        var command = new AddTransactionCommand(request);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        var saved = (await uow.FakeTransactions.GetAllAsync()).Single();
        Assert.Equal(request.FromAssetId, saved.FromAssetId);
        Assert.Equal(request.ToAssetId, saved.ToAssetId);
        Assert.Equal(10000, saved.AmountSpent);
        Assert.Equal(1, uow.SaveChangesCallCount); // persisted exactly once
        
        Assert.NotNull(result);
        Assert.Equal(saved.Id, result.Id);
    }
}
