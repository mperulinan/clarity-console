using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.DTOs;
using Portfolio.Domain.Interfaces;
using Portfolio.Application.Interfaces;
using Portfolio.Domain.ValueObjects;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;

namespace Portfolio.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AssetController(IAssetRepository assetRepository, IAssetSearchProvider searchProvider, IAssetSynchronizationService syncService, ITransactionRepository transactionRepository) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AssetDto>>> GetAssets()
    {
        var assets = await assetRepository.GetAllAsync();
        return Ok(assets.Select(a => new AssetDto
        {
            Id = a.Id,
            Symbol = a.Symbol,
            Name = a.Name,
            ExternalId = a.ExternalId,
            ImageUrl = a.ImageUrl,
            Type = a.Type.Value
        }));
    }

    [HttpGet("fiat-currencies")]
    public ActionResult<IEnumerable<AssetDto>> GetFiatCurrencies()
    {
        var fiats = FiatCurrency.List.Select(f => new AssetDto
        {
            Id = f.Id,
            Symbol = f.Name,
            Name = f.Name,
            ImageUrl = f.ImageUrl,
            Type = AssetType.Fiat.Value
        });
        return Ok(fiats);
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<AssetDto>>> SearchAssets([FromQuery] string query, [FromQuery] string? type = null)
    {
        // 1. Search local DB
        var allLocalAssets = await assetRepository.GetAllAsync();
        var localAssets = allLocalAssets.Where(a =>
            (string.IsNullOrEmpty(query) ||
             a.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
             a.Symbol.Contains(query, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrEmpty(type) || a.Type.Value.Equals(type, StringComparison.OrdinalIgnoreCase))
        ).ToList();

        // 2. Search external API (only if query is provided and long enough)
        IEnumerable<SearchAssetResult> externalAssets = [];
        if (!string.IsNullOrWhiteSpace(query) && query.Length >= 2)
        {
            var assetType = string.IsNullOrEmpty(type) ? AssetType.Crypto : AssetType.FromValue(type);
            externalAssets = await searchProvider.SearchAssetsAsync(assetType, query);
        }

        // 3. Compute transaction counts for all local assets
        var allTransactions = await transactionRepository.GetAllAsync();
        Dictionary<Guid, int> transactionCounts = [];
        foreach (Transaction tx in allTransactions)
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

        // 4. Build merged list — local assets first, deduplicated by ExternalId
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
            MarketCapRank = null // DB assets do not have a market cap rank
        }).ToList();

        var externalDtos = externalAssets
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
        var sorted = localDtos
            .OrderByDescending(a => a.TransactionCount > 0)  // locals with txs first
            .ThenByDescending(a => a.TransactionCount)        // most-used first within tier 1
            .ThenBy(a => a.Name)                              // alphabetical within tier 2
            .Concat(externalDtos
                .OrderBy(a => a.MarketCapRank ?? int.MaxValue) // nulls last
            );

        return Ok(sorted);
    }

    [HttpPost("sync")]
    public async Task<ActionResult<AssetDto>> SyncAsset([FromBody] AssetDto request)
    {
        if (string.IsNullOrWhiteSpace(request?.ExternalId))
            return BadRequest("ExternalId is required for synchronization.");

        var existingAssets = await assetRepository.GetByExternalIdsAsync(new[] { request.ExternalId });
        var existingAsset = existingAssets.FirstOrDefault();
        
        if (existingAsset != null) 
        {
            return Ok(new AssetDto
            {
                Id = existingAsset.Id,
                Symbol = existingAsset.Symbol,
                Name = existingAsset.Name,
                ExternalId = existingAsset.ExternalId,
                ImageUrl = existingAsset.ImageUrl,
                Type = existingAsset.Type.Value
            });
        }

        var assetType = string.IsNullOrEmpty(request.Type) ? AssetType.Crypto : AssetType.FromValue(request.Type);
        var newAsset = new Asset(request.Symbol, request.Name, request.ExternalId, request.ImageUrl, assetType);
        
        await syncService.SynchronizeCatalogAsync(new[] { newAsset });

        // Fetch it back to get the DB generated/assigned ID if the sync service created one
        var syncedAssets = await assetRepository.GetByExternalIdsAsync(new[] { request.ExternalId });
        var syncedAsset = syncedAssets.FirstOrDefault();

        if (syncedAsset == null)
            return StatusCode(500, "Failed to synchronize asset.");

        return Ok(new AssetDto
        {
            Id = syncedAsset.Id,
            Symbol = syncedAsset.Symbol,
            Name = syncedAsset.Name,
            ExternalId = syncedAsset.ExternalId,
            ImageUrl = syncedAsset.ImageUrl,
            Type = syncedAsset.Type.Value
        });
    }
}
