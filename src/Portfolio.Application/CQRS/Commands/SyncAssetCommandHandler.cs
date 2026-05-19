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
        var request = command.Asset;
        
        if (string.IsNullOrWhiteSpace(request?.ExternalId))
            throw new ArgumentException("ExternalId is required for synchronization.", nameof(command));

        var existingAsset = (await unitOfWork.Assets.GetByExternalIdsAsync([request.ExternalId])).FirstOrDefault();

        // If asset already exists and has a logo, we're done.
        if (existingAsset != null && !string.IsNullOrEmpty(existingAsset.ImageUrl))
        {
            return existingAsset.ToDto();
        }

        var assetType = string.IsNullOrEmpty(request.Type) ? AssetType.Crypto : AssetType.FromValue(request.Type);
        
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
