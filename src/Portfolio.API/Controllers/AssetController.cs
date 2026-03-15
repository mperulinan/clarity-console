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
public class AssetController(IAssetRepository assetRepository, IAssetSearchProvider searchProvider, IAssetSynchronizationService syncService) : ControllerBase
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

        // 2. Search external API (only if query is provided and longer)
        IEnumerable<Asset> externalAssets = Array.Empty<Asset>();
        if (!string.IsNullOrWhiteSpace(query) && query.Length >= 2)
        {
            var assetType = string.IsNullOrEmpty(type) ? AssetType.Crypto : AssetType.FromValue(type);
            externalAssets = await searchProvider.SearchAssetsAsync(assetType, query);
        }

        // Merge, prioritizing local assets
        var localExternalIds = localAssets.Where(a => !string.IsNullOrEmpty(a.ExternalId))
                                          .Select(a => a.ExternalId!)
                                          .ToHashSet();
        
        var mergedAssets = localAssets.Select(a => new AssetDto
        {
            Id = a.Id,
            Symbol = a.Symbol,
            Name = a.Name,
            ExternalId = a.ExternalId,
            ImageUrl = a.ImageUrl,
            Type = a.Type.Value
        }).ToList();

        foreach (var ext in externalAssets)
        {
            if (string.IsNullOrEmpty(ext.ExternalId) || !localExternalIds.Contains(ext.ExternalId))
            {
                mergedAssets.Add(new AssetDto
                {
                    Id = Guid.Empty, // Default Guid for unsynced
                    Symbol = ext.Symbol,
                    Name = ext.Name,
                    ExternalId = ext.ExternalId,
                    ImageUrl = ext.ImageUrl,
                    Type = ext.Type.Value
                });
            }
        }

        return Ok(mergedAssets);
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
