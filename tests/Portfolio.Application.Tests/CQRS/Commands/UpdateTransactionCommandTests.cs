using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Portfolio.Application.Interfaces;
using Portfolio.Application.CQRS.Commands;
using Portfolio.Application.DTOs;
using Portfolio.Application.Tests.Fakes;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;

namespace Portfolio.Application.Tests.CQRS.Commands;

public class UpdateTransactionCommandTests
{
    [Fact]
    public async Task Handle_ShouldUpdateTransaction_WhenItExists()
    {
        // Arrange
        var uow = new FakeUnitOfWork();
        var backgroundQueue = Substitute.For<IBackgroundTaskQueue>();
        var handler = new UpdateTransactionCommandHandler(uow, backgroundQueue, NullLogger<UpdateTransactionCommandHandler>.Instance);

        var existingTx = new Transaction(
            date: DateTime.UtcNow.AddDays(-1),
            transactionType: TransactionType.Deposit,
            fromAssetId: null,
            toAssetId: FiatCurrency.TaxCurrency.Id,
            amountSpent: 100,
            amountReceived: 2,
            spotPriceUSD: 50,
            spotPriceEUR: 50,
            fee: 0,
            feeAssetId: null,
            feeSpotPriceUSD: null,
            feeSpotPriceEUR: null,
            usdEurExchangeRate: 1m,
            spotPriceInputCurrency: FiatCurrency.USD,
            feePriceInputCurrency: null,
            notes: null
        );
        // We set Id via reflection
        typeof(Transaction).GetProperty(nameof(Transaction.Id))?.SetValue(existingTx, 1);
        await uow.Transactions.AddAsync(existingTx);

        var newDate = DateTime.UtcNow;
        var request = new NewTransactionRequest
        {
            Date = newDate,
            TransactionTypeCode = TransactionType.Deposit,
            FromAssetId = existingTx.FromAssetId,
            ToAssetId = existingTx.ToAssetId,
            AmountSpent = 150,
            AmountReceived = 3,
            SpotPriceUSD = 50,
            SpotPriceInputCurrency = "USD"
        };
        var command = new UpdateTransactionCommand(1, request);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        var updated = await uow.Transactions.GetByIdAsync(1);
        Assert.NotNull(updated);
        Assert.Equal(150, updated.AmountSpent);
        Assert.Equal(3, updated.AmountReceived);
        Assert.Equal(newDate, updated.Date);
        Assert.Equal(1, uow.SaveChangesCallCount);
        
        Assert.NotNull(result);
        Assert.Equal(updated.Id, result.Id);
        
        await backgroundQueue.Received(1).QueueBackgroundWorkItemAsync(
            Arg.Is<SyncTransactionExchangeRateCommand>(c => c.TransactionId == updated.Id));
    }

    [Fact]
    public async Task Handle_ShouldThrowKeyNotFoundException_WhenTransactionDoesNotExist()
    {
        // Arrange
        var uow = new FakeUnitOfWork();
        var backgroundQueue = Substitute.For<IBackgroundTaskQueue>();
        var handler = new UpdateTransactionCommandHandler(uow, backgroundQueue, NullLogger<UpdateTransactionCommandHandler>.Instance);
        var request = new NewTransactionRequest
        {
            Date = DateTime.UtcNow,
            TransactionTypeCode = TransactionType.Deposit,
            ToAssetId = FiatCurrency.TaxCurrency.Id,
            AmountReceived = 100,
            SpotPriceUSD = 1,
            SpotPriceInputCurrency = "USD"
        };
        var command = new UpdateTransactionCommand(999, request);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ShouldThrowArgumentException_WhenSpotPriceInputCurrencyIsInvalid()
    {
        // Arrange
        var uow = new FakeUnitOfWork();
        var backgroundQueue = Substitute.For<IBackgroundTaskQueue>();
        var handler = new UpdateTransactionCommandHandler(uow, backgroundQueue, NullLogger<UpdateTransactionCommandHandler>.Instance);

        var existingTx = new Transaction(
            date: DateTime.UtcNow.AddDays(-1),
            transactionType: TransactionType.Deposit,
            fromAssetId: null,
            toAssetId: FiatCurrency.TaxCurrency.Id,
            amountSpent: 0,
            amountReceived: 100,
            spotPriceUSD: 1,
            spotPriceEUR: 0.9m,
            fee: 0,
            feeAssetId: null,
            feeSpotPriceUSD: null,
            feeSpotPriceEUR: null,
            usdEurExchangeRate: 1.1m,
            spotPriceInputCurrency: FiatCurrency.USD,
            feePriceInputCurrency: null,
            notes: null
        );
        typeof(Transaction).GetProperty(nameof(Transaction.Id))?.SetValue(existingTx, 1);
        await uow.Transactions.AddAsync(existingTx);

        var request = new NewTransactionRequest
        {
            Date = DateTime.UtcNow,
            TransactionTypeCode = TransactionType.Deposit,
            ToAssetId = FiatCurrency.TaxCurrency.Id,
            AmountReceived = 100,
            SpotPriceUSD = 1,
            SpotPriceInputCurrency = "GBP" // unsupported
        };
        var command = new UpdateTransactionCommand(1, request);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            handler.Handle(command, CancellationToken.None));
    }
}
