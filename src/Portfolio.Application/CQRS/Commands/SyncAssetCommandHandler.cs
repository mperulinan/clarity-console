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
    IServiceProvider serviceProvider,
    ILogger<SyncAssetCommandHandler> logger)
    : IRequestHandler<SyncAssetCommand, AssetDto>
{
    public async Task<AssetDto> Handle(SyncAssetCommand command, CancellationToken cancellationToken)
    {
        var request = command.Asset;
        
        if (string.IsNullOrWhiteSpace(request?.ExternalId))
            throw new ArgumentException("ExternalId is required for synchronization.", nameof(command));

        var existingAssets = await unitOfWork.Assets.GetByExternalIdsAsync([request.ExternalId]);
        var existingAsset = existingAssets.FirstOrDefault();

        if (existingAsset != null)
        {
            // If the existing record already has a logo, return it immediately.
            if (!string.IsNullOrEmpty(existingAsset.ImageUrl))
                return existingAsset.ToDto();
        }

        var assetType = string.IsNullOrEmpty(request.Type) ? AssetType.Crypto : AssetType.FromValue(request.Type);

        // Lazy logo fetch: if the user selected a Stock or ETF with no image, fetch it now.
        string? imageUrl = request.ImageUrl ?? existingAsset?.ImageUrl;
        if (imageUrl == null && assetType is { } t && (t == AssetType.Stock || t == AssetType.Etf))
        {
            var logoProvider = serviceProvider.GetKeyedService<IAssetLogoProvider>(t.Value);
            if (logoProvider != null)
            {
                imageUrl = await logoProvider.GetLogoUrlAsync(request.Symbol);
            }
        }

        if (existingAsset != null)
        {
            // Asset exists but had no logo — patch it and return.
            existingAsset.UpdateMetadata(existingAsset.Symbol, existingAsset.Name, imageUrl);
            await unitOfWork.SaveChangesAsync();
            logger.LogInformation("Updated metadata for existing asset {Symbol} ({Id}).", existingAsset.Symbol, existingAsset.Id);
            return existingAsset.ToDto();
        }

        var newAsset = new Asset(request.Symbol, request.Name, request.ExternalId, imageUrl, assetType);

        await syncService.SynchronizeCatalogAsync([newAsset]);

        var syncedAssets = await unitOfWork.Assets.GetByExternalIdsAsync([request.ExternalId]);
        var syncedAsset = syncedAssets.FirstOrDefault();

        if (syncedAsset == null)
            throw new InvalidOperationException($"Failed to synchronize asset {request.Symbol}.");

        logger.LogInformation("Synchronized new asset {Symbol} ({Id}).", syncedAsset.Symbol, syncedAsset.Id);
        return syncedAsset.ToDto();
    }
}
