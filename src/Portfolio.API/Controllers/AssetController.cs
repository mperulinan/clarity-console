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
}
