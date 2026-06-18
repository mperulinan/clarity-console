using Microsoft.Extensions.Logging.Abstractions;
using Portfolio.Application.CQRS.Commands;
using Portfolio.Application.Tests.Fakes;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;

namespace Portfolio.Application.Tests.CQRS.Commands;

public class DeleteTransactionCommandTests
{
    [Fact]
    public async Task Handle_ShouldDeleteTransaction_WhenItExists()
    {
        // Arrange
        var uow = new FakeUnitOfWork();
        var handler = new DeleteTransactionCommandHandler(uow, NullLogger<DeleteTransactionCommandHandler>.Instance);

        var existingTx = new Transaction(
            date: DateTime.UtcNow,
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
        // We set Id via reflection since it's typically set by DB or protected
        typeof(Transaction).GetProperty(nameof(Transaction.Id))?.SetValue(existingTx, 1);
        
        await uow.Transactions.AddAsync(existingTx);

        var command = new DeleteTransactionCommand(1);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        var result = await uow.Transactions.GetByIdAsync(1);
        Assert.Null(result);
        Assert.Equal(1, uow.SaveChangesCallCount);
    }
}
