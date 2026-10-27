using MediatR;
using Portfolio.Application.DTOs;

namespace Portfolio.Application.CQRS.Commands;

public record PreviewImportTransactionsCommand(
    string BrokerKey,
    Stream FileStream
) : IRequest<ImportTransactionsPreviewDto>;
