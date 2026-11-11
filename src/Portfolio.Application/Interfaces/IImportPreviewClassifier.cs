using Portfolio.Application.DTOs;
using Portfolio.Application.Models;

namespace Portfolio.Application.Interfaces;

/// <summary>
/// Classifies raw imported transaction rows into preview rows with duplicate detection,
/// local asset resolution, and auto-create flags.
/// </summary>
public interface IImportPreviewClassifier
{
    /// <summary>
    /// Classifies each raw row and returns the corresponding preview rows.
    /// </summary>
    /// <param name="rawRows">Parsed rows from the broker CSV.</param>
    /// <param name="isinToAsset">External search results keyed by ISIN (null value = unresolvable).</param>
    /// <param name="existingBrokerTransactionIds">Set of broker transaction IDs already in the database.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<ImportedTransactionRowPreview>> ClassifyRowsAsync(
        IReadOnlyList<ImportedTransactionRow> rawRows,
        IReadOnlyDictionary<string, AssetDto?> isinToAsset,
        IReadOnlySet<string> existingBrokerTransactionIds,
        CancellationToken cancellationToken = default);
}

