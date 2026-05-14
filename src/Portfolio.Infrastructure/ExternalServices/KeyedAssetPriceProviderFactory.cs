using Microsoft.Extensions.DependencyInjection;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;

namespace Portfolio.Infrastructure.ExternalServices;

/// <summary>
/// Resolves the correct <see cref="IAssetPriceProvider"/> by looking up
/// the keyed registration that matches the given <see cref="AssetType"/>.
/// </summary>
public class KeyedAssetPriceProviderFactory(IServiceProvider serviceProvider) : IAssetPriceProviderFactory
{
    public IAssetPriceProvider? GetProvider(AssetType type)
        => serviceProvider.GetKeyedService<IAssetPriceProvider>(type.Value);
}
