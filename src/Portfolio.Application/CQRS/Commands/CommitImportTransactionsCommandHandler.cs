using MediatR;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using Portfolio.Application.Mappers;
using Portfolio.Application.Models;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Interfaces;

namespace Portfolio.Application.CQRS.Commands;

/// <summary>
/// Orchestrates the two-phase commit of imported broker transactions:
/// resolves missing assets via IImportAssetResolver, maps rows to domain Transaction entities via ImportedTransactionMapper,
/// persists the batch atomically, enqueues exchange rate background syncs, and invalidates the preview session.
/// </summary>
public class CommitImportTransactionsCommandHandler(
    IUnitOfWork uow,
    IImportAssetResolver assetResolver,
    IBackgroundTaskQueue backgroundQueue,
    IImportPreviewStore previewStore,
    IEnumerable<ITransactionImportParser>? parsers = null)
    : IRequestHandler<CommitImportTransactionsCommand, ImportTransactionsResultDto>
{
    public async Task<ImportTransactionsResultDto> Handle(
        CommitImportTransactionsCommand command, 
        CancellationToken cancellationToken)
    {
        var session = previewStore.Get(command.PreviewToken)
            ?? throw new InvalidOperationException("The import preview session has expired or does not exist. Please upload the CSV again.");

        var brokerParser = parsers?.FirstOrDefault(p => string.Equals(p.BrokerKey, session.BrokerKey, StringComparison.OrdinalIgnoreCase));
        var brokerName = brokerParser?.DisplayName ?? session.BrokerKey.ToUpperInvariant();

        var excludedSet = command.ExcludedBrokerTransactionIds?.ToHashSet() ?? [];
        var overrides = command.OverriddenSymbols ?? new Dictionary<string, string>();

        var actionableRows = session.Rows
            .Select(r => overrides.TryGetValue(r.Raw.BrokerTransactionId, out var sym) && !string.IsNullOrWhiteSpace(sym)
                ? r with { ResolvedSymbol = sym.Trim().ToUpperInvariant(), IsUnresolvable = false, ResolvedAssetId = null }
                : r)
            .Where(r => !r.IsDuplicate && !r.IsUnresolvable && !excludedSet.Contains(r.Raw.BrokerTransactionId))
            .ToList();

        var skippedCount = session.Rows.Count - actionableRows.Count;

        // Phase A: Resolve/auto-create any missing assets
        var assetsToSync = actionableRows
            .Where(r => r.ResolvedAssetId == null && !string.IsNullOrWhiteSpace(r.ResolvedSymbol))
            .Select(r => r.ResolvedSymbol!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var symbolToAssetId = await assetResolver.ResolveMissingAssetsAsync(assetsToSync, cancellationToken);

        // Phase B: Map rows to Transaction entities and persist batch
        var createdTransactions = new List<Transaction>();
        var warnings = new List<string>();

        foreach (var previewRow in actionableRows)
        {
            var targetAssetId = previewRow.ResolvedAssetId;

            if (!targetAssetId.HasValue && previewRow.ResolvedSymbol != null && symbolToAssetId.TryGetValue(previewRow.ResolvedSymbol, out var syncedId))
            {
                targetAssetId = syncedId;
            }

            if (!targetAssetId.HasValue)
            {
                var raw = previewRow.Raw;
                warnings.Add($"Skipped order '{raw.BrokerTransactionId}' because asset '{raw.ProductName}' ({raw.Isin}) could not be resolved.");
                skippedCount++;
                continue;
            }

            var transaction = ImportedTransactionMapper.ToTransaction(previewRow.Raw, targetAssetId.Value, brokerName);
            await uow.Transactions.AddAsync(transaction);
            createdTransactions.Add(transaction);
        }

        if (createdTransactions.Count > 0)
        {
            await uow.SaveChangesAsync(cancellationToken);

            foreach (var tx in createdTransactions)
            {
                await backgroundQueue.QueueBackgroundWorkItemAsync(new SyncTransactionExchangeRateCommand(tx.Id));
            }
        }

        previewStore.Remove(command.PreviewToken);

        return new ImportTransactionsResultDto(
            ImportedCount: createdTransactions.Count,
            SkippedCount: skippedCount,
            Warnings: warnings
        );
    }
}
