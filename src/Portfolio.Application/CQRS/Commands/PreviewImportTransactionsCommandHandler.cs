using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using Portfolio.Application.Models;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.ValueObjects;

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
    IAssetSearchService searchService,
    IMemoryCache cache,
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

        // Cache resolution for distinct ISINs to minimize external searches
        var distinctIsins = rawRows.Select(r => r.Isin).Where(i => !string.IsNullOrWhiteSpace(i)).Distinct();
        var isinMap = new Dictionary<string, AssetDto?>(StringComparer.OrdinalIgnoreCase);

        foreach (var isin in distinctIsins)
        {
            try
            {
                var searchResults = await searchService.SearchAsync(isin, AssetType.Stock.Value);
                var match = searchResults.FirstOrDefault();
                isinMap[isin] = match;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to search for asset with ISIN '{Isin}'", isin);
                isinMap[isin] = null;
            }
        }

        var previewRows = new List<ImportedTransactionRowPreview>();

        foreach (var row in rawRows)
        {
            var isDuplicate = existingBrokerIds.Contains(row.BrokerTransactionId);
            isinMap.TryGetValue(row.Isin, out var resolvedSearchAsset);

            string? resolvedSymbol = null;
            Guid? resolvedAssetId = null;
            var willAutoCreate = false;
            var isUnresolvable = false;
            string? warningMessage = null;

            if (isDuplicate)
            {
                warningMessage = "Transaction already imported.";
            }

            if (resolvedSearchAsset != null)
            {
                resolvedSymbol = resolvedSearchAsset.Symbol;
                var localAsset = await uow.Assets.GetBySymbolAsync(resolvedSearchAsset.Symbol, AssetType.Stock, cancellationToken);
                if (localAsset != null)
                {
                    resolvedAssetId = localAsset.Id;
                }
                else
                {
                    willAutoCreate = true;
                }
            }
            else
            {
                isUnresolvable = true;
                warningMessage ??= $"Could not resolve ticker symbol for ISIN '{row.Isin}'.";
            }

            previewRows.Add(new ImportedTransactionRowPreview(
                Raw: row,
                ResolvedSymbol: resolvedSymbol,
                ResolvedAssetId: resolvedAssetId,
                WillAutoCreate: willAutoCreate,
                IsDuplicate: isDuplicate,
                IsUnresolvable: isUnresolvable,
                WarningMessage: warningMessage
            ));
        }

        var previewToken = Guid.NewGuid().ToString();
        var session = new ImportPreviewSession(command.BrokerKey, previewRows, DateTimeOffset.UtcNow);
        cache.Set(previewToken, session, TimeSpan.FromMinutes(10));

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
