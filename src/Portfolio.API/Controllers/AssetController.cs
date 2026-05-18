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
    IUnitOfWork unitOfWork,
    IAssetSynchronizationService syncService,
    [FromKeyedServices("STOCK")] IAssetLogoProvider stockLogoProvider,
    [FromKeyedServices("ETF")] IAssetLogoProvider etfLogoProvider) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AssetDto>>> GetAssets()
    {
        var assets = await unitOfWork.Assets.GetAllAsync();
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

        var existingAssets = await unitOfWork.Assets.GetByExternalIdsAsync([request.ExternalId]);
        var existingAsset = existingAssets.FirstOrDefault();

        if (existingAsset != null)
        {
            // If the existing record already has a logo, return it immediately.
            if (!string.IsNullOrEmpty(existingAsset.ImageUrl))
                return Ok(existingAsset.ToDto());

            // Otherwise fall through to the lazy logo-fetch below and update the record.
        }

        var assetType = string.IsNullOrEmpty(request.Type) ? AssetType.Crypto : AssetType.FromValue(request.Type);

        // Lazy logo fetch: if the user selected a Stock or ETF with no image, fetch it now.
        string? imageUrl = request.ImageUrl ?? existingAsset?.ImageUrl;
        if (imageUrl == null && assetType is { } t && (t == AssetType.Stock || t == AssetType.Etf))
        {
            var logoProvider = t == AssetType.Stock ? stockLogoProvider : etfLogoProvider;
            imageUrl = await logoProvider.GetLogoUrlAsync(request.Symbol);
        }

        if (existingAsset != null)
        {
            // Asset exists but had no logo — patch it and return.
            existingAsset.UpdateMetadata(existingAsset.Symbol, existingAsset.Name, imageUrl);
            await unitOfWork.SaveChangesAsync();
            return Ok(existingAsset.ToDto());
        }

        var newAsset = new Asset(request.Symbol, request.Name, request.ExternalId, imageUrl, assetType);

        await syncService.SynchronizeCatalogAsync([newAsset]);

        var syncedAssets = await unitOfWork.Assets.GetByExternalIdsAsync([request.ExternalId]);
        var syncedAsset = syncedAssets.FirstOrDefault();

        if (syncedAsset == null)
            return StatusCode(500, "Failed to synchronize asset.");

        return Ok(syncedAsset.ToDto());
    }
}
