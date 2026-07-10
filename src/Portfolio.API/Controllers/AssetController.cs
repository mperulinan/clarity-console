using MediatR;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.CQRS.Commands;
using Portfolio.Application.CQRS.Queries;
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

    [HttpGet("default-currency")]
    public ActionResult<AssetDto> GetDefaultCurrency()
        => Ok(AssetMapper.ToDto(FiatCurrency.DefaultDisplayCurrency));

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<AssetDto>>> SearchAssets([FromQuery] string query, [FromQuery] string? type = null)
        => Ok(await assetSearchService.SearchAsync(query, type));

    [HttpPost("sync")]
    public async Task<ActionResult<AssetDto>> SyncAsset([FromBody] AssetDto request)
        => Ok(await mediator.Send(new SyncAssetCommand(request)));

    [HttpGet("{id:guid}/price")]
    public async Task<ActionResult<decimal>> GetSpotPrice(Guid id, [FromQuery] string fiatCurrency, [FromQuery] DateTime? date = null)
    {
        var price = await mediator.Send(new GetAssetSpotPriceQuery(id, fiatCurrency, date));
        
        if (price == null)
        {
            return NotFound();
        }
        
        return Ok(price);
    }
}
