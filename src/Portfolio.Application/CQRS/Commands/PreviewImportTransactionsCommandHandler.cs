using MediatR;
using Microsoft.Extensions.Logging;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using Portfolio.Application.Models;
using Portfolio.Domain.Interfaces;

namespace Portfolio.Application.CQRS.Commands;

/// <summary>
/// Handles CSV import preview for external broker transactions.
///
/// Note on Exchange Ticker Resolution:
/// TwelveData symbol search matches on ISIN and may return multiple listings across global exchanges
/// (e.g. SAP on XETRA vs SAP ADR on NYSE).
/// Resolution prioritizes the candidate matching the CSV's reference exchange ('Bolsa de referencia').
/// In the local database, assets are identified by Symbol and AssetType. Dual-listed assets sharing the same ticker
/// across different foreign exchanges are treated under the primary resolved symbol for v1 personal portfolios.
/// </summary>
public class PreviewImportTransactionsCommandHandler(
    IEnumerable<ITransactionImportParser> parsers,
    IUnitOfWork uow,
    IImportTickerResolver tickerResolver,
    IImportPreviewClassifier previewClassifier,
    IImportPreviewStore previewStore,
    ILogger<PreviewImportTransactionsCommandHandler> logger)
    : IRequestHandler<PreviewImportTransactionsCommand, ImportTransactionsPreviewDto>
{
    public async Task<ImportTransactionsPreviewDto> Handle(
        PreviewImportTransactionsCommand command,
        CancellationToken cancellationToken)
    {
        var parser = parsers.FirstOrDefault(p => string.Equals(p.BrokerKey, command.BrokerKey, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"No transaction import parser registered for broker '{command.BrokerKey}'.");

        logger.LogInformation("Parsing import stream for broker '{BrokerKey}'", command.BrokerKey);
        var rawRows = await parser.ParseAsync(command.FileStream, cancellationToken);

        if (rawRows.Count == 0)
        {
            return new ImportTransactionsPreviewDto(
                PreviewToken: string.Empty,
                Rows: Array.Empty<ImportedTransactionRowPreview>(),
                TotalRows: 0,
                DuplicateRows: 0,
                UnresolvableRows: 0,
                ReadyToImportRows: 0
            );
        }

        var brokerIds = rawRows.Select(r => r.BrokerTransactionId).Distinct().ToList();
        var existingBrokerIds = await uow.Transactions.GetExistingBrokerTransactionIdsAsync(brokerIds, cancellationToken);

        var isinMap = await tickerResolver.ResolveIsinsAsync(rawRows, cancellationToken);

        var previewRows = await previewClassifier.ClassifyRowsAsync(
            rawRows,
            isinMap,
            existingBrokerIds,
            cancellationToken);

        var previewToken = Guid.NewGuid().ToString();
        var session = new ImportPreviewSession(command.BrokerKey, previewRows, DateTimeOffset.UtcNow);

        previewStore.Set(previewToken, session);

        return new ImportTransactionsPreviewDto(
            PreviewToken: previewToken,
            Rows: previewRows,
            TotalRows: previewRows.Count,
            DuplicateRows: previewRows.Count(r => r.IsDuplicate),
            UnresolvableRows: previewRows.Count(r => r.IsUnresolvable),
            ReadyToImportRows: previewRows.Count(r => !r.IsDuplicate && !r.IsUnresolvable)
        );
    }
}
