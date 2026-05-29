using MediatR;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.CQRS.Commands;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using Portfolio.Application.Mappers;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;

namespace Portfolio.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AssetController(
    IAssetSearchService assetSearchService,
    IAssetPriceProviderFactory priceProviderFactory,
    IUnitOfWork unitOfWork,
    ISender mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AssetDto>>> GetAssets()
    {
        var assets = await unitOfWork.Assets.GetAllAsync();
        return Ok(assets.Select(AssetMapper.ToDto));
    }

    [HttpGet("fiat-currencies")]
    public ActionResult<IEnumerable<AssetDto>> GetFiatCurrencies()
        => Ok(FiatCurrency.List.Select(AssetMapper.ToDto));

    [HttpGet("tax-currency")]
    public ActionResult<AssetDto> GetTaxCurrency()
        => Ok(AssetMapper.ToDto(FiatCurrency.TaxCurrency));

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<AssetDto>>> SearchAssets([FromQuery] string query, [FromQuery] string? type = null)
        => Ok(await assetSearchService.SearchAsync(query, type));

    [HttpPost("sync")]
    public async Task<ActionResult<AssetDto>> SyncAsset([FromBody] AssetDto request)
    {
        if (string.IsNullOrWhiteSpace(request?.ExternalId))
            return BadRequest("ExternalId is required for synchronization.");

        var result = await mediator.Send(new SyncAssetCommand(request));
        return Ok(result);
    }

    [HttpGet("{id:guid}/price")]
    public async Task<ActionResult<decimal>> GetSpotPrice(Guid id, [FromQuery] string fiatCurrency, [FromQuery] DateTime? date = null)
    {
        if (!FiatCurrency.TryFromValue(fiatCurrency.ToLowerInvariant(), out var currency))
        {
            return BadRequest("Invalid fiat currency.");
        }

        var asset = await unitOfWork.Assets.GetByIdAsync(id);
        if (asset == null || string.IsNullOrWhiteSpace(asset.ExternalId))
        {
            return NotFound("Asset not found or missing external identifier.");
        }

        var priceProvider = priceProviderFactory.GetProvider(asset.Type);
        if (priceProvider == null)
        {
            return BadRequest($"No price provider available for asset type '{asset.Type.Name}'.");
        }

        if (date.HasValue)
        {
            var historicalPrice = await priceProvider.GetHistoricalPriceAsync(asset.ExternalId, currency, date.Value);
            if (historicalPrice.HasValue)
            {
                return Ok(historicalPrice.Value);
            }
            return NotFound();
        }
        else
        {
            var prices = await priceProvider.GetPricesAsync([asset.ExternalId], currency);
            if (prices.TryGetValue(asset.ExternalId, out var price))
            {
                return Ok(price);
            }
            return NotFound();
        }
    }
}
