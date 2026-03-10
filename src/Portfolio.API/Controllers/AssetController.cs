using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.DTOs;
using Portfolio.Domain.Interfaces;

namespace Portfolio.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AssetController(IAssetRepository assetRepository) : ControllerBase
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
}
