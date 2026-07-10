using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using Portfolio.Application.Mappers;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;

namespace Portfolio.Application.CQRS.Commands;

public class SyncAssetCommandHandler(
    IUnitOfWork unitOfWork,
    IAssetSynchronizationService syncService,
    IEnumerable<IAssetLogoProvider> logoProviders,
    ILogger<SyncAssetCommandHandler> logger)
    : IRequestHandler<SyncAssetCommand, AssetDto>
{
    public async Task<AssetDto> Handle(SyncAssetCommand command, CancellationToken cancellationToken)
    {
        var request = command.Asset ?? throw new ArgumentException("Asset is required for synchronization.", nameof(command));

        var assetType = string.IsNullOrEmpty(request.Type) ? AssetType.Crypto : AssetType.FromValue(request.Type);

        if (!assetType.RequiresMarketData)
        {
            // Local custom asset: bypass external sync, just ensure it exists locally by Symbol.
            var allAssets = await unitOfWork.Assets.GetAllAsync();
            var existingCustomAsset = allAssets.FirstOrDefault(a => a.Type == assetType && a.Symbol.Equals(request.Symbol, StringComparison.OrdinalIgnoreCase));
            
            return existingCustomAsset?.ToDto() ?? throw new InvalidOperationException($"Asset {request.Symbol} not found.");
        }

        if (string.IsNullOrWhiteSpace(request.ExternalId))
        {
            throw new ArgumentException("ExternalId is required for synchronization of external assets.", nameof(command));
        }

        var existingAssets = await unitOfWork.Assets.GetByExternalIdsAsync([request.ExternalId]);
        var existingAsset = existingAssets.FirstOrDefault(a => a.Type == assetType);

        // If asset already exists and has a logo, we're done.
        if (existingAsset != null && !string.IsNullOrEmpty(existingAsset.ImageUrl))
        {
            return existingAsset.ToDto();
        }

        // Attempt lazy logo fetch if missing
        string? imageUrl = await ResolveLogoUrlAsync(request.Symbol, request.ImageUrl ?? existingAsset?.ImageUrl, assetType);

        if (existingAsset != null)
        {
            existingAsset.UpdateMetadata(existingAsset.Symbol, existingAsset.Name, imageUrl);
            await unitOfWork.SaveChangesAsync();
            logger.LogInformation("Updated metadata for existing asset {Symbol} ({Id}).", existingAsset.Symbol, existingAsset.Id);
            return existingAsset.ToDto();
        }

        var newAsset = new Asset(request.Symbol, request.Name, request.ExternalId, imageUrl, assetType);
        await syncService.SynchronizeCatalogAsync([newAsset]);

        var syncedAsset = (await unitOfWork.Assets.GetByExternalIdsAsync([request.ExternalId])).FirstOrDefault();
        if (syncedAsset == null)
            throw new InvalidOperationException($"Failed to synchronize asset {request.Symbol}.");

        logger.LogInformation("Synchronized new asset {Symbol} ({Id}).", syncedAsset.Symbol, syncedAsset.Id);
        return syncedAsset.ToDto();
    }

    private async Task<string?> ResolveLogoUrlAsync(string symbol, string? currentImageUrl, AssetType type)
    {
        if (currentImageUrl != null) return currentImageUrl;

        var provider = logoProviders.FirstOrDefault(p => p.Supports(type));
        if (provider != null)
        {
            return await provider.GetLogoUrlAsync(symbol);
        }

        return null;
    }
}
