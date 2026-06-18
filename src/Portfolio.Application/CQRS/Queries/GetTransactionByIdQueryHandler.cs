using MediatR;
using Portfolio.Application.DTOs;
using Portfolio.Application.Mappers;
using Portfolio.Domain.Interfaces;

namespace Portfolio.Application.CQRS.Queries;

public class GetTransactionByIdQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetTransactionByIdQuery, TransactionDto?>
{
    public async Task<TransactionDto?> Handle(GetTransactionByIdQuery query, CancellationToken cancellationToken)
    {
        var transaction = await uow.Transactions.GetByIdAsync(query.Id);
        return transaction?.ToDto();
    }
}
