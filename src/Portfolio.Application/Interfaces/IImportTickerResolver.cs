using Portfolio.Application.DTOs;
using Portfolio.Application.Models;

namespace Portfolio.Application.Interfaces;

/// <summary>
/// Resolves ISIN codes to external asset data by searching the asset search service.
/// Applies a product-name fallback and exchange-preference prioritization heuristic.
/// </summary>
public interface IImportTickerResolver
{
    /// <summary>
    /// Returns a case-insensitive map of ISIN → best-matching <see cref="AssetDto"/> (or <c>null</c>
    /// when the ISIN cannot be resolved externally).
    /// </summary>
    Task<IReadOnlyDictionary<string, AssetDto?>> ResolveIsinsAsync(
        IEnumerable<ImportedTransactionRow> rows,
        CancellationToken cancellationToken = default);
}

