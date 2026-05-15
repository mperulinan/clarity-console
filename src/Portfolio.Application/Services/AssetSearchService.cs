using Microsoft.Extensions.DependencyInjection;
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
    IServiceProvider serviceProvider,
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

        var results = MergeAndSort(localDtos, externalDtos);

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
    // Step 2 — External provider fan-out
    // -------------------------------------------------------------------------

    private async Task<List<SearchAssetResult>> SearchExternalProvidersAsync(string? query, string? type)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < ExternalSearchMinLength)
            return [];

        var targetTypes = ResolveTargetTypes(type);
        var searchTasks = targetTypes.Select(assetType => SearchProviderAsync(assetType, query)).ToList();

        logger.LogDebug("Dispatching {Count} external search task(s).", searchTasks.Count);

        var resultArrays = await Task.WhenAll(searchTasks);
        var externalResults = resultArrays.SelectMany(r => r).ToList();

        logger.LogDebug("Found {Count} external matches.", externalResults.Count);
        return externalResults;
    }

    private static IEnumerable<AssetType> ResolveTargetTypes(string? type) =>
        string.IsNullOrEmpty(type)
            ? AssetType.List.Where(t => t.CanBeSearchedExternally)
            : [AssetType.FromValue(type)];

    private Task<IEnumerable<SearchAssetResult>> SearchProviderAsync(AssetType assetType, string query)
    {
        var provider = serviceProvider.GetKeyedService<IAssetSearchProvider>(assetType.Value);
        return provider is not null
            ? provider.SearchAssetsAsync(assetType, query)
            : Task.FromResult(Enumerable.Empty<SearchAssetResult>());
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
    // Step 5 — Three-tier sort and merge
    //   Tier 1 — DB assets with transactions (most-used first)
    //   Tier 2 — DB assets with 0 transactions (alphabetical by name)
    //   Tier 3 — External assets (ascending market-cap rank, nulls last)
    // -------------------------------------------------------------------------

    private static List<AssetDto> MergeAndSort(
        IEnumerable<AssetDto> localDtos,
        IEnumerable<AssetDto> externalDtos) =>
        [
            .. localDtos
                .OrderByDescending(a => a.TransactionCount > 0)
                .ThenByDescending(a => a.TransactionCount)
                .ThenBy(a => a.Name)
            ,
            .. externalDtos.OrderBy(a => a.MarketCapRank ?? int.MaxValue),
        ];
}
