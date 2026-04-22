using Microsoft.Extensions.Logging;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using Portfolio.Application.Mappers;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;

namespace Portfolio.Application.Services;

public class AssetSearchService(
    IUnitOfWork unitOfWork,
    IAssetSearchProvider searchProvider,
    ILogger<AssetSearchService> logger) : IAssetSearchService
{
    public async Task<IEnumerable<AssetDto>> SearchAsync(string query, string? type = null)
    {
        logger.LogInformation("Asset search requested: '{Query}' (Filter: {Type})", query ?? "", type ?? "None");

        // 1. Search local DB
        var allLocalAssets = await unitOfWork.Assets.GetAllAsync();
        var localAssets = allLocalAssets.Where(a =>
            (string.IsNullOrEmpty(query) ||
             a.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
             a.Symbol.Contains(query, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrEmpty(type) || a.Type.Value.Equals(type, StringComparison.OrdinalIgnoreCase))
        ).ToList();

        logger.LogDebug("Found {Count} local matches.", localAssets.Count);

        // 2. Search external provider (CoinGecko), only when query is long enough
        IEnumerable<SearchAssetResult> externalResults = [];
        if (!string.IsNullOrWhiteSpace(query) && query.Length >= 2)
        {
            logger.LogDebug("Query length {Length} qualifies for external search.", query.Length);
            var assetType = string.IsNullOrEmpty(type) ? AssetType.Crypto : AssetType.FromValue(type);
            externalResults = await searchProvider.SearchAssetsAsync(assetType, query);
            logger.LogDebug("Found {Count} external matches.", externalResults.Count());
        }

        // 3. Compute per-asset transaction counts (counts appearances across From, To, and Fee)
        var allTransactions = await unitOfWork.Transactions.GetAllAsync();
        Dictionary<Guid, int> transactionCounts = [];
        foreach (var tx in allTransactions)
        {
            if (tx.FromAssetId.HasValue)
            {
                transactionCounts[tx.FromAssetId.Value] = transactionCounts.GetValueOrDefault(tx.FromAssetId.Value) + 1;
            }
            if (tx.ToAssetId.HasValue)
            {
                transactionCounts[tx.ToAssetId.Value] = transactionCounts.GetValueOrDefault(tx.ToAssetId.Value) + 1;
            }
            if (tx.FeeAssetId.HasValue)
            {
                transactionCounts[tx.FeeAssetId.Value] = transactionCounts.GetValueOrDefault(tx.FeeAssetId.Value) + 1;
            }
        }

        // 4. Build local DTOs and deduplicate external results against them
        var localExternalIds = localAssets
            .Where(a => !string.IsNullOrEmpty(a.ExternalId))
            .Select(a => a.ExternalId!)
            .ToHashSet();

        var localDtos = localAssets.Select(a =>
        {
            var dto = a.ToDto();
            dto.TransactionCount = transactionCounts.GetValueOrDefault(a.Id, 0);
            return dto;
        }).ToList();

        var externalDtos = externalResults
            .Where(r => string.IsNullOrEmpty(r.Asset.ExternalId) || !localExternalIds.Contains(r.Asset.ExternalId))
            .Select(r =>
            {
                var dto = r.Asset.ToDto();
                dto.MarketCapRank = r.MarketCapRank;
                return dto;
            });

        // 5. Three-tier sort:
        //    Tier 1 - DB assets with transactions, most-used first
        //    Tier 2 - DB assets with 0 transactions, alphabetical by name
        //    Tier 3 - CoinGecko-only assets, ascending market_cap_rank (nulls last)
        var results = localDtos
            .OrderByDescending(a => a.TransactionCount > 0)
            .ThenByDescending(a => a.TransactionCount)
            .ThenBy(a => a.Name)
            .Concat(externalDtos
                .OrderBy(a => a.MarketCapRank ?? int.MaxValue)
            ).ToList();

        logger.LogInformation("Search complete. Returning {TotalCount} total assets.", results.Count);
        return results;
    }
}
