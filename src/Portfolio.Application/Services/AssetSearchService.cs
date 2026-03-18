using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Application.Services;

public class AssetSearchService(
    IAssetRepository assetRepository,
    IAssetSearchProvider searchProvider,
    ITransactionRepository transactionRepository) : IAssetSearchService
{
    public async Task<IEnumerable<AssetDto>> SearchAsync(string query, string? type = null)
    {
        // 1. Search local DB
        var allLocalAssets = await assetRepository.GetAllAsync();
        var localAssets = allLocalAssets.Where(a =>
            (string.IsNullOrEmpty(query) ||
             a.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
             a.Symbol.Contains(query, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrEmpty(type) || a.Type.Value.Equals(type, StringComparison.OrdinalIgnoreCase))
        ).ToList();

        // 2. Search external provider (CoinGecko), only when query is long enough
        IEnumerable<SearchAssetResult> externalResults = [];
        if (!string.IsNullOrWhiteSpace(query) && query.Length >= 2)
        {
            var assetType = string.IsNullOrEmpty(type) ? AssetType.Crypto : AssetType.FromValue(type);
            externalResults = await searchProvider.SearchAssetsAsync(assetType, query);
        }

        // 3. Compute per-asset transaction counts (counts appearances across From, To, and Fee)
        var allTransactions = await transactionRepository.GetAllAsync();
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

        var localDtos = localAssets.Select(a => new AssetDto
        {
            Id = a.Id,
            Symbol = a.Symbol,
            Name = a.Name,
            ExternalId = a.ExternalId,
            ImageUrl = a.ImageUrl,
            Type = a.Type.Value,
            TransactionCount = transactionCounts.GetValueOrDefault(a.Id, 0),
            MarketCapRank = null
        }).ToList();

        var externalDtos = externalResults
            .Where(r => string.IsNullOrEmpty(r.Asset.ExternalId) || !localExternalIds.Contains(r.Asset.ExternalId))
            .Select(r => new AssetDto
            {
                Id = Guid.Empty,
                Symbol = r.Asset.Symbol,
                Name = r.Asset.Name,
                ExternalId = r.Asset.ExternalId,
                ImageUrl = r.Asset.ImageUrl,
                Type = r.Asset.Type.Value,
                TransactionCount = 0,
                MarketCapRank = r.MarketCapRank
            });

        // 5. Three-tier sort:
        //    Tier 1 — DB assets with transactions, most-used first
        //    Tier 2 — DB assets with 0 transactions, alphabetical by name
        //    Tier 3 — CoinGecko-only assets, ascending market_cap_rank (nulls last)
        return localDtos
            .OrderByDescending(a => a.TransactionCount > 0)
            .ThenByDescending(a => a.TransactionCount)
            .ThenBy(a => a.Name)
            .Concat(externalDtos
                .OrderBy(a => a.MarketCapRank ?? int.MaxValue)
            );
    }
}
