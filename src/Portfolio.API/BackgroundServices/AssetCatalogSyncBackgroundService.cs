using Portfolio.Domain.Interfaces;
using Portfolio.Application.Interfaces;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;

namespace Portfolio.API.BackgroundServices;

public class AssetCatalogSyncBackgroundService(
    IServiceProvider serviceProvider,
    ILogger<AssetCatalogSyncBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = serviceProvider.CreateAsyncScope();
                var catalogProviders = scope.ServiceProvider.GetRequiredService<IEnumerable<IAssetCatalogProvider>>();
                var syncService = scope.ServiceProvider.GetRequiredService<IAssetSynchronizationService>();
                var assetRepository = scope.ServiceProvider.GetRequiredService<IAssetRepository>();

                var allDbAssets = await assetRepository.GetAllAsync();
                var topAssets = new List<Asset>();

                foreach (var provider in catalogProviders)
                {
                    logger.LogInformation("Starting Asset Catalog Sync from provider: {ProviderName}...", provider.GetType().Name);

                    // Fetch top assets for supported types
                    foreach (var type in AssetType.List)
                    {
                        var assetsForType = (await provider.GetTopAssetsAsync(type, 250)).ToList();
                        if (assetsForType.Count > 0)
                        {
                            topAssets.AddRange(assetsForType);

                            // Find DB assets of this type not covered by the top N
                            var syncedExternalIds = new HashSet<string>(assetsForType.Select(a => a.ExternalId!), StringComparer.OrdinalIgnoreCase);
                            var remainingExternalIds = allDbAssets
                                .Where(a => a.Type == type && a.ExternalId != null && !syncedExternalIds.Contains(a.ExternalId))
                                .Select(a => a.ExternalId!)
                                .ToList();

                            if (remainingExternalIds.Count > 0)
                            {
                                logger.LogInformation("Fetching {Count} additional DB {Type} assets from {ProviderName}...", remainingExternalIds.Count, type.Value, provider.GetType().Name);
                                var additionalAssets = await provider.GetAssetsByExternalIdsAsync(type, remainingExternalIds);
                                topAssets.AddRange(additionalAssets);
                            }
                        }
                    }
                }

                // Single sync pass for the combined list
                int updatedCount = await syncService.SynchronizeCatalogAsync(topAssets);
                logger.LogInformation("Asset Catalog Sync complete. {Count} assets inserted/updated.", updatedCount);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while syncing the Asset Catalog. Will try again next cycle.");
            }

            // Wait 12 hours before next sync
            await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
        }
    }
}
