using MediatR;
using Portfolio.Application.DTOs;

namespace Portfolio.Application.CQRS.Queries;

public record GetTransactionByIdQuery(int Id) : IRequest<TransactionDto?>;
