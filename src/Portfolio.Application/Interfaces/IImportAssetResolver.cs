namespace Portfolio.Application.Interfaces;

/// <summary>
/// Resolves asset IDs for imported transaction symbols, synchronizing/auto-creating missing stock assets when necessary.
/// </summary>
public interface IImportAssetResolver
{
    Task<IReadOnlyDictionary<string, Guid>> ResolveMissingAssetsAsync(
        IEnumerable<string> symbols, 
        CancellationToken cancellationToken = default);
}

