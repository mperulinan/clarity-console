using MediatR;
using Portfolio.Application.DTOs;

namespace Portfolio.Application.CQRS.Queries;

public record GetProcessedTransactionByIdQuery(int Id) : IRequest<ProcessedTransactionDto?>;
