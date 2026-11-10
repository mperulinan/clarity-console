namespace Portfolio.Application.Models;

/// <summary>
/// Symbol and optional asset type to resolve or auto-create during import commit.
/// </summary>
public record ImportAssetToResolve(string Symbol, string? AssetType = null);

