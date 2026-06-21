using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Portfolio.Application.CQRS.Commands;
using Portfolio.Application.Tests.Fakes;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;

namespace Portfolio.Application.Tests.CQRS.Commands;

public class SyncTransactionExchangeRateCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldUpdateExchangeRate_WhenTransactionExists()
    {
        // Arrange
        var uow = new FakeUnitOfWork();
        var exchangeRateProvider = Substitute.For<IExchangeRateProvider>();
        
        var date = DateTime.UtcNow.AddDays(-1);
        var tx = new Transaction(date, TransactionType.Swap, Guid.NewGuid(), Guid.NewGuid(), 100, 1, 1m, null, 0, null, null, null, null, FiatCurrency.USD, null, null);
        
        // Reflection to set Id to 1
        typeof(Transaction).GetProperty(nameof(Transaction.Id))?.SetValue(tx, 1);
        await uow.Transactions.AddAsync(tx);

        exchangeRateProvider.GetUsdEurRateAsync(date).Returns(Task.FromResult(0.85m));

        var handler = new SyncTransactionExchangeRateCommandHandler(uow, exchangeRateProvider, NullLogger<SyncTransactionExchangeRateCommandHandler>.Instance);
        var command = new SyncTransactionExchangeRateCommand(1);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var savedTx = await uow.Transactions.GetByIdAsync(1);
        Assert.NotNull(savedTx);
        Assert.Equal(0.85m, savedTx.UsdEurExchangeRate);
        Assert.Equal(0.85m, savedTx.SpotPriceEUR); // Assuming SpotPriceUSD = 1 and usdEurExchangeRate = 0.85
        Assert.Equal(1, uow.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_ShouldGracefullySkip_WhenProviderThrowsNotSupportedException()
    {
        // Arrange
        var uow = new FakeUnitOfWork();
        var exchangeRateProvider = Substitute.For<IExchangeRateProvider>();
        
        var today = DateTime.UtcNow;
        var tx = new Transaction(today, TransactionType.Swap, Guid.NewGuid(), Guid.NewGuid(), 100, 1, 1m, null, 0, null, null, null, null, FiatCurrency.USD, null, null);
        typeof(Transaction).GetProperty(nameof(Transaction.Id))?.SetValue(tx, 1);
        await uow.Transactions.AddAsync(tx);

        exchangeRateProvider.GetUsdEurRateAsync(today).Returns(Task.FromException<decimal>(new NotSupportedException("Current day not supported")));

        var handler = new SyncTransactionExchangeRateCommandHandler(uow, exchangeRateProvider, NullLogger<SyncTransactionExchangeRateCommandHandler>.Instance);
        var command = new SyncTransactionExchangeRateCommand(1);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var savedTx = await uow.Transactions.GetByIdAsync(1);
        Assert.Null(savedTx!.UsdEurExchangeRate);
        Assert.Equal(0, uow.SaveChangesCallCount);
    }
}
