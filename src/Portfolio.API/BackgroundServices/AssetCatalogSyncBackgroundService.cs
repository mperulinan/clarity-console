using Portfolio.Domain.Interfaces;
using Portfolio.Application.Interfaces;
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
                var catalogProvider = scope.ServiceProvider.GetRequiredService<IAssetCatalogProvider>();
                var syncService = scope.ServiceProvider.GetRequiredService<IAssetSynchronizationService>();
                var assetRepository = scope.ServiceProvider.GetRequiredService<IAssetRepository>();

                // 1. Fetch top 250 from CoinGecko
                logger.LogInformation("Starting Asset Catalog Sync from CoinGecko...");
                var topAssets = (await catalogProvider.GetTopAssetsAsync(AssetType.Crypto, 250)).ToList();

                // 2. Find DB crypto assets not covered by the top 250
                var syncedExternalIds = new HashSet<string>(topAssets.Select(a => a.ExternalId!), StringComparer.OrdinalIgnoreCase);
                var allDbAssets = await assetRepository.GetAllAsync();
                var remainingExternalIds = allDbAssets
                    .Where(a => a.Type == AssetType.Crypto && a.ExternalId != null && !syncedExternalIds.Contains(a.ExternalId))
                    .Select(a => a.ExternalId!)
                    .ToList();

                if (remainingExternalIds.Count > 0)
                {
                    logger.LogInformation("Fetching {Count} additional DB crypto assets from CoinGecko...", remainingExternalIds.Count);
                    var additionalAssets = await catalogProvider.GetAssetsByExternalIdsAsync(AssetType.Crypto, remainingExternalIds);
                    topAssets.AddRange(additionalAssets);
                }

                // 3. Single sync pass for the combined list
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
