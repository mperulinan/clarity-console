using MediatR;
using Portfolio.Application.DTOs;

namespace Portfolio.Application.CQRS.Queries;

public record GetProcessedTransactionsQuery(string? Currency = null) : IRequest<IEnumerable<ProcessedTransactionDto>>;
