using MediatR;

namespace Portfolio.Application.CQRS.Commands;

public record SyncTransactionExchangeRateCommand(int TransactionId) : IRequest;
