using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using Portfolio.Application.Models;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;

namespace Portfolio.Application.CQRS.Commands;

/// <summary>
/// Handles the two-phase commit of imported broker transactions.
/// Phase A ensures all uncreated assets are resolved and persisted via SyncAssetCommand before transaction batching.
/// Phase B constructs Transaction entities (with domain-derived spot prices) and saves the batch via a single SaveChangesAsync call.
/// </summary>
public class CommitImportTransactionsCommandHandler(
    IUnitOfWork uow,
    IMediator mediator,
    IBackgroundTaskQueue backgroundQueue,
    IMemoryCache cache,
    ILogger<CommitImportTransactionsCommandHandler> logger)
    : IRequestHandler<CommitImportTransactionsCommand, ImportTransactionsResultDto>
{
    public async Task<ImportTransactionsResultDto> Handle(
        CommitImportTransactionsCommand command, 
        CancellationToken cancellationToken)
    {
        if (!cache.TryGetValue<ImportPreviewSession>(command.PreviewToken, out var session) || session == null)
        {
            throw new InvalidOperationException("The import preview session has expired or does not exist. Please upload the CSV again.");
        }

        var excludedSet = command.ExcludedBrokerTransactionIds?.ToHashSet() ?? [];
        var warnings = new List<string>();

        var actionableRows = session.Rows
            .Where(r => !r.IsDuplicate && !r.IsUnresolvable && !excludedSet.Contains(r.Raw.BrokerTransactionId))
            .ToList();

        var skippedCount = session.Rows.Count - actionableRows.Count;

        // Phase A: Pre-sync assets via SyncAssetCommand so they exist and are saved before building transactions
        var assetsToSync = actionableRows
            .Where(r => r.ResolvedAssetId == null && !string.IsNullOrWhiteSpace(r.ResolvedSymbol))
            .Select(r => r.ResolvedSymbol!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var symbolToAssetId = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        foreach (var symbol in assetsToSync)
        {
            var existingAsset = await uow.Assets.GetBySymbolAsync(symbol, AssetType.Stock, cancellationToken);
            if (existingAsset == null)
            {
                logger.LogInformation("Auto-creating asset for symbol '{Symbol}'", symbol);
                await mediator.Send(new SyncAssetCommand(new AssetDto
                {
                    Symbol = symbol,
                    Type = AssetType.Stock.Value,
                    Name = symbol,
                    ExternalId = symbol
                }), cancellationToken);

                existingAsset = await uow.Assets.GetBySymbolAsync(symbol, AssetType.Stock, cancellationToken);
            }

            if (existingAsset != null)
            {
                symbolToAssetId[symbol] = existingAsset.Id;
            }
            else
            {
                logger.LogWarning("Failed to resolve newly synced asset for symbol '{Symbol}'", symbol);
            }
        }

        // Phase B: Transaction Batch Creation & Single SaveChangesAsync
        var createdTransactions = new List<Transaction>();

        foreach (var previewRow in actionableRows)
        {
            var row = previewRow.Raw;
            var targetAssetId = previewRow.ResolvedAssetId;

            if (!targetAssetId.HasValue && previewRow.ResolvedSymbol != null && symbolToAssetId.TryGetValue(previewRow.ResolvedSymbol, out var syncedId))
            {
                targetAssetId = syncedId;
            }

            if (!targetAssetId.HasValue)
            {
                warnings.Add($"Skipped order '{row.BrokerTransactionId}' because asset '{row.ProductName}' ({row.Isin}) could not be resolved.");
                skippedCount++;
                continue;
            }

            Guid fromAssetId;
            Guid toAssetId;
            decimal amountSpent;
            decimal amountReceived;

            if (row.Quantity > 0)
            {
                // BUY: Swap EUR -> Stock
                fromAssetId = FiatCurrency.EUR.Id;
                toAssetId = targetAssetId.Value;
                amountSpent = Math.Abs(row.TotalEur);
                amountReceived = Math.Abs(row.Quantity);
            }
            else
            {
                // SELL: Swap Stock -> EUR
                fromAssetId = targetAssetId.Value;
                toAssetId = FiatCurrency.EUR.Id;
                amountSpent = Math.Abs(row.Quantity);
                amountReceived = Math.Abs(row.TotalEur);
            }

            var transaction = new Transaction(
                date: row.Date,
                transactionType: TransactionType.Swap,
                fromAssetId: fromAssetId,
                toAssetId: toAssetId,
                amountSpent: amountSpent,
                amountReceived: amountReceived,
                spotPriceUSD: null,
                spotPriceEUR: null,
                fee: row.FeeEur ?? 0m,
                feeAssetId: FiatCurrency.EUR.Id,
                feeSpotPriceUSD: null,
                feeSpotPriceEUR: null,
                usdEurExchangeRate: null,
                spotPriceInputCurrency: null,
                feePriceInputCurrency: null,
                notes: $"Imported from DEGIRO ({row.ProductName}). Order ID: {row.BrokerTransactionId}",
                brokerTransactionId: row.BrokerTransactionId
            );

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

        cache.Remove(command.PreviewToken);

        return new ImportTransactionsResultDto(
            ImportedCount: createdTransactions.Count,
            SkippedCount: skippedCount,
            Warnings: warnings
        );
    }
}
