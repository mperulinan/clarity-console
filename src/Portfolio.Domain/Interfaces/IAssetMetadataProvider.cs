namespace Portfolio.Domain.Interfaces;

/// <summary>
/// Provides metadata (e.g. image URLs) for crypto assets from an external source.
/// </summary>
public interface IAssetMetadataProvider
{
    /// <summary>
    /// Returns a map of assetId → image URL for the given asset IDs.
    /// Assets not found by the provider are omitted from the dictionary.
    /// </summary>
    Task<Dictionary<string, string>> GetAssetImageUrlsAsync(IEnumerable<string> assetIds);
}
