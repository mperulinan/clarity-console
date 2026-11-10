using Portfolio.Application.Models;

namespace Portfolio.Application.Interfaces;

/// <summary>
/// Resolves asset IDs for imported transaction symbols, synchronizing/auto-creating missing stock or ETF assets when necessary.
/// </summary>
public interface IImportAssetResolver
{
    Task<IReadOnlyDictionary<string, Guid>> ResolveMissingAssetsAsync(
        IEnumerable<ImportAssetToResolve> assets, 
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, Guid>> ResolveMissingAssetsAsync(
        IEnumerable<string> symbols, 
        CancellationToken cancellationToken = default) =>
        ResolveMissingAssetsAsync(symbols.Select(s => new ImportAssetToResolve(s)), cancellationToken);
}
