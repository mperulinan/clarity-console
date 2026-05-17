using Microsoft.Extensions.Logging;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using Portfolio.Application.Mappers;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;

namespace Portfolio.Application.Services;

public class AssetSearchService(
    IUnitOfWork unitOfWork,
    IEnumerable<IAssetSearchProvider> searchProviders,
    ILogger<AssetSearchService> logger) : IAssetSearchService
{
    // Minimum query length required to trigger external provider searches.
    private const int ExternalSearchMinLength = 2;

    // -------------------------------------------------------------------------
    // Public contract
    // -------------------------------------------------------------------------

    public async Task<IEnumerable<AssetDto>> SearchAsync(string query, string? type = null)
    {
        logger.LogInformation("Asset search requested: '{Query}' (Filter: {Type})", query ?? "", type ?? "None");

        var localAssets        = await SearchLocalAssetsAsync(query, type);
        var externalResults    = await SearchExternalProvidersAsync(query, type);
        var transactionCounts  = await BuildTransactionCountsAsync();

        var localExternalIds = GetLocalExternalIds(localAssets);
        var localDtos        = BuildLocalDtos(localAssets, transactionCounts);
        var externalDtos     = BuildExternalDtos(externalResults, localExternalIds);

        var results = MergeAndSort(localDtos, externalDtos, query);

        logger.LogInformation("Search complete. Returning {TotalCount} total assets.", results.Count);
        return results;
    }

    // -------------------------------------------------------------------------
    // Step 1 — Local database search
    // -------------------------------------------------------------------------

    private async Task<List<Asset>> SearchLocalAssetsAsync(string? query, string? type)
    {
        var allAssets = await unitOfWork.Assets.GetAllAsync();

        var matches = allAssets.Where(a =>
            MatchesQuery(a, query) &&
            MatchesType(a, type)
        ).ToList();

        logger.LogDebug("Found {Count} local matches.", matches.Count);
        return matches;
    }

    private static bool MatchesQuery(Asset asset, string? query) =>
        string.IsNullOrEmpty(query) ||
        asset.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
        asset.Symbol.Contains(query, StringComparison.OrdinalIgnoreCase);

    private static bool MatchesType(Asset asset, string? type) =>
        string.IsNullOrEmpty(type) ||
        asset.Type.Value.Equals(type, StringComparison.OrdinalIgnoreCase);

    // -------------------------------------------------------------------------
    // Step 2 — External provider fan-out (one call per provider, in parallel)
    // -------------------------------------------------------------------------

    private async Task<List<SearchAssetResult>> SearchExternalProvidersAsync(string? query, string? type)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < ExternalSearchMinLength)
            return [];

        // Fan out to every registered provider concurrently
        var searchTasks = searchProviders
            .Select(provider => provider.SearchAssetsAsync(query))
            .ToList();

        logger.LogDebug("Dispatching {Count} external search task(s).", searchTasks.Count);

        var resultArrays = await Task.WhenAll(searchTasks);
        var externalResults = resultArrays.SelectMany(r => r).ToList();

        // If the caller specified a type filter, apply it after merging
        if (!string.IsNullOrEmpty(type))
            externalResults = externalResults
                .Where(r => r.Asset.Type.Value.Equals(type, StringComparison.OrdinalIgnoreCase))
                .ToList();

        logger.LogDebug("Found {Count} external matches.", externalResults.Count);
        return externalResults;
    }

    // -------------------------------------------------------------------------
    // Step 3 — Transaction count aggregation
    // -------------------------------------------------------------------------

    private async Task<Dictionary<Guid, int>> BuildTransactionCountsAsync()
    {
        var allTransactions = await unitOfWork.Transactions.GetAllAsync();
        var counts = new Dictionary<Guid, int>();

        foreach (var tx in allTransactions)
        {
            IncrementCount(counts, tx.FromAssetId);
            IncrementCount(counts, tx.ToAssetId);
            IncrementCount(counts, tx.FeeAssetId);
        }

        return counts;
    }

    private static void IncrementCount(Dictionary<Guid, int> counts, Guid? assetId)
    {
        if (assetId.HasValue)
            counts[assetId.Value] = counts.GetValueOrDefault(assetId.Value) + 1;
    }

    // -------------------------------------------------------------------------
    // Step 4 — DTO construction and deduplication
    // -------------------------------------------------------------------------

    private static HashSet<string> GetLocalExternalIds(IEnumerable<Asset> localAssets) =>
        [.. localAssets
            .Where(a => !string.IsNullOrEmpty(a.ExternalId))
            .Select(a => a.ExternalId!)];

    private static List<AssetDto> BuildLocalDtos(
        IEnumerable<Asset> localAssets,
        Dictionary<Guid, int> transactionCounts) =>
        [.. localAssets.Select(a =>
        {
            var dto = a.ToDto();
            dto.TransactionCount = transactionCounts.GetValueOrDefault(a.Id, 0);
            return dto;
        })];

    private static IEnumerable<AssetDto> BuildExternalDtos(
        IEnumerable<SearchAssetResult> externalResults,
        HashSet<string> localExternalIds) =>
        externalResults
            .Where(r => string.IsNullOrEmpty(r.Asset.ExternalId) || !localExternalIds.Contains(r.Asset.ExternalId))
            .DistinctBy(r => r.Asset.ExternalId)
            .Select(r =>
            {
                var dto = r.Asset.ToDto();
                dto.MarketCapRank = r.MarketCapRank;
                return dto;
            });

    // -------------------------------------------------------------------------
    // Step 5 — Relevance Sort
    // -------------------------------------------------------------------------

    private static List<AssetDto> MergeAndSort(
        IEnumerable<AssetDto> localDtos,
        IEnumerable<AssetDto> externalDtos,
        string? query)
    {
        var exactMatchQuery = query?.Trim() ?? "";
        bool IsExactMatch(AssetDto a) => string.Equals(a.Symbol, exactMatchQuery, StringComparison.OrdinalIgnoreCase);

        var localList = localDtos.ToList();
        var externalList = externalDtos.ToList();

        // Tier 1: Local assets with transactions (Highest priority)
        var tier1 = localList
            .Where(a => a.TransactionCount > 0)
            .OrderByDescending(IsExactMatch)
            .ThenByDescending(a => a.TransactionCount);

        // Tier 2: Exact symbol matches (from both local without txs and external APIs)
        var remainingLocal = localList.Where(a => a.TransactionCount == 0).ToList();
        
        var exactMatches = remainingLocal.Where(IsExactMatch)
            .Concat(externalList.Where(IsExactMatch))
            .OrderBy(a => a.MarketCapRank ?? int.MaxValue);

        // Tier 3: Remaining local assets (alphabetical)
        var tier3 = remainingLocal
            .Where(a => !IsExactMatch(a))
            .OrderBy(a => a.Name);

        // Tier 4: Remaining external assets
        // Sorts CoinGecko assets by rank, and safely appends TwelveData assets (which have null rank)
        // at the end in their native API relevance order.
        var tier4 = externalList
            .Where(a => !IsExactMatch(a))
            .OrderBy(a => a.MarketCapRank ?? int.MaxValue);

        return
        [
            .. tier1,
            .. exactMatches,
            .. tier3,
            .. tier4
        ];
    }
}
