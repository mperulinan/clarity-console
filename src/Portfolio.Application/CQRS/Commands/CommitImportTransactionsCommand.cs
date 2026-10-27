using MediatR;
using Portfolio.Application.DTOs;

namespace Portfolio.Application.CQRS.Commands;

public record CommitImportTransactionsCommand(
    string BrokerKey,
    string PreviewToken,
    IReadOnlyList<string>? ExcludedBrokerTransactionIds = null
) : IRequest<ImportTransactionsResultDto>;
