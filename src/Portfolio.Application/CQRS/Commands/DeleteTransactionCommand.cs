using MediatR;
using Microsoft.Extensions.Logging;
using Portfolio.Domain.Interfaces;

namespace Portfolio.Application.CQRS.Commands;

public record DeleteTransactionCommand(int Id) : IRequest;

public class DeleteTransactionCommandHandler(IUnitOfWork uow, ILogger<DeleteTransactionCommandHandler> logger)
    : IRequestHandler<DeleteTransactionCommand>
{
    public async Task Handle(DeleteTransactionCommand command, CancellationToken cancellationToken)
    {
        var existing = await uow.Transactions.GetByIdAsync(command.Id);
        if (existing == null)
        {
            logger.LogWarning("Transaction {TransactionId} not found for deletion.", command.Id);
            return;
        }

        await uow.Transactions.DeleteAsync(command.Id);
        await uow.SaveChangesAsync(cancellationToken);
        
        logger.LogInformation("Deleted transaction {TransactionId}.", command.Id);
    }
}
