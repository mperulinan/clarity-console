using Portfolio.Application.DTOs;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;

namespace Portfolio.Application.Mappers;

/// <summary>
/// Centralized mapper from domain <see cref="Asset"/> and <see cref="FiatCurrency"/> to <see cref="AssetDto"/>.
/// All code that needs to project an Asset to a DTO should go through here.
/// </summary>
public static class AssetMapper
{
    public static AssetDto ToDto(this Asset asset) => new()
    {
        Id = asset.Id,
        Symbol = asset.Symbol,
        Name = asset.Name,
        ExternalId = asset.ExternalId,
        ImageUrl = asset.ImageUrl,
        Type = asset.Type.Value
    };

    public static AssetDto ToDto(this FiatCurrency fiat) => new()
    {
        Id = fiat.Id,
        Symbol = fiat.Symbol,
        Name = fiat.Name,
        ImageUrl = fiat.ImageUrl,
        Type = AssetType.Fiat.Value
    };
}
