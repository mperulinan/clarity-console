using MediatR;
using Microsoft.Extensions.Logging;
using Portfolio.Domain.Interfaces;

namespace Portfolio.Application.CQRS.Commands;

public class SyncTransactionExchangeRateCommandHandler(
    IUnitOfWork uow, 
    IExchangeRateProvider exchangeRateProvider,
    ILogger<SyncTransactionExchangeRateCommandHandler> logger)
    : IRequestHandler<SyncTransactionExchangeRateCommand>
{
    public async Task Handle(SyncTransactionExchangeRateCommand command, CancellationToken cancellationToken)
    {
        var tx = await uow.Transactions.GetByIdAsync(command.TransactionId);
        if (tx == null)
        {
            logger.LogWarning("Transaction {TransactionId} not found when syncing exchange rate.", command.TransactionId);
            return;
        }

        if (!tx.HasIncompleteExchangeRates)
        {
            logger.LogInformation("Transaction {TransactionId} already has complete exchange rates.", command.TransactionId);
            return;
        }

        try
        {
            var rate = await exchangeRateProvider.GetUsdEurRateAsync(tx.Date);
            tx.UpdateExchangeRates(rate);
            
            await uow.Transactions.UpdateAsync(tx);
            await uow.SaveChangesAsync(cancellationToken);
            
            logger.LogInformation("Successfully synced exchange rate for transaction {TransactionId}.", command.TransactionId);
        }
        catch (NotSupportedException)
        {
            logger.LogInformation("Exchange rate for date {Date} is not yet available for transaction {TransactionId}.", tx.Date, command.TransactionId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to sync exchange rate for transaction {TransactionId}.", command.TransactionId);
        }
    }
}
