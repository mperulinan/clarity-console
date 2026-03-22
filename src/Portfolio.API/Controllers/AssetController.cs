using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;

namespace Portfolio.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AssetController(
    IAssetSearchService assetSearchService,
    IAssetRepository assetRepository,
    IAssetSynchronizationService syncService) : ControllerBase
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
            Symbol = f.Symbol,
            Name = f.Name,
            ImageUrl = f.ImageUrl,
            Type = AssetType.Fiat.Value
        });
        return Ok(fiats);
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<AssetDto>>> SearchAssets([FromQuery] string query, [FromQuery] string? type = null)
        => Ok(await assetSearchService.SearchAsync(query, type));

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
