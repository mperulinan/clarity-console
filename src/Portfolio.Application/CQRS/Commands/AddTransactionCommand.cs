using MediatR;
using Portfolio.Application.DTOs;

namespace Portfolio.Application.CQRS.Commands;

public record AddTransactionCommand(NewTransactionRequest Request) : IRequest<TransactionDto>;
