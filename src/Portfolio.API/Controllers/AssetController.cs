using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using Portfolio.Application.Mappers;
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
        return Ok(assets.Select(AssetMapper.ToDto));
    }

    [HttpGet("fiat-currencies")]
    public ActionResult<IEnumerable<AssetDto>> GetFiatCurrencies()
    {
        return Ok(FiatCurrency.List.Select(AssetMapper.ToDto));
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
            return Ok(existingAsset.ToDto());
        }

        var assetType = string.IsNullOrEmpty(request.Type) ? AssetType.Crypto : AssetType.FromValue(request.Type);
        var newAsset = new Asset(request.Symbol, request.Name, request.ExternalId, request.ImageUrl, assetType);

        await syncService.SynchronizeCatalogAsync(new[] { newAsset });

        var syncedAssets = await assetRepository.GetByExternalIdsAsync(new[] { request.ExternalId });
        var syncedAsset = syncedAssets.FirstOrDefault();

        if (syncedAsset == null)
            return StatusCode(500, "Failed to synchronize asset.");

        return Ok(syncedAsset.ToDto());
    }
}
