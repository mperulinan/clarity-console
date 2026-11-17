using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using Portfolio.Application.Models;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;

namespace Portfolio.Application.Services;

/// <summary>
/// Classifies each raw imported row into a preview row by:
///   - Flagging duplicates from <paramref name="existingBrokerTransactionIds"/>.
///   - Short-circuiting to a fiat asset for cash rows (non-Swap types with no ISIN).
///   - Resolving the asset via the ISIN-to-asset map from <see cref="IImportTickerResolver"/>.
///   - Checking local DB for the asset (Stock first, then ETF) to populate <c>ResolvedAssetId</c>.
///   - Marking rows as <c>WillAutoCreate</c> when the asset exists externally but not locally.
///   - Marking rows as <c>IsUnresolvable</c> when the ISIN could not be resolved at all.
/// </summary>
public class ImportPreviewClassifier(IUnitOfWork uow) : IImportPreviewClassifier
{
    public async Task<IReadOnlyList<ImportedTransactionRowPreview>> ClassifyRowsAsync(
        IReadOnlyList<ImportedTransactionRow> rawRows,
        IReadOnlyDictionary<string, AssetDto?> isinToAsset,
        IReadOnlySet<string> existingBrokerTransactionIds,
        CancellationToken cancellationToken = default)
    {
        var previewRows = new List<ImportedTransactionRowPreview>(rawRows.Count);

        foreach (var row in rawRows)
        {
            var isDuplicate = existingBrokerTransactionIds.Contains(row.BrokerTransactionId);
            isinToAsset.TryGetValue(row.Isin, out var resolvedSearchAsset);

            string? resolvedSymbol = null;
            Guid? resolvedAssetId = null;
            string? resolvedAssetType = null;
            var willAutoCreate = false;
            var isUnresolvable = false;
            string? warningMessage = null;

            if (isDuplicate)
            {
                warningMessage = "Transaction already imported.";
            }

            if (row.TransactionType != null &&
                row.TransactionType != TransactionType.Swap &&
                string.IsNullOrWhiteSpace(row.Isin))
            {
                var fiatCurrency = string.Equals(row.PriceCurrency, "USD", StringComparison.OrdinalIgnoreCase)
                    ? FiatCurrency.USD
                    : FiatCurrency.EUR;

                resolvedAssetId = fiatCurrency.Id;
                resolvedSymbol = fiatCurrency.Symbol;
                resolvedAssetType = AssetType.Fiat.Value;
                willAutoCreate = false;
                isUnresolvable = false;
            }
            else if (resolvedSearchAsset != null)
            {
                resolvedSymbol = resolvedSearchAsset.Symbol;
                resolvedAssetType = resolvedSearchAsset.Type;

                var assetType = ResolveAssetType(resolvedSearchAsset.Type);
                var localAsset = await uow.Assets.GetBySymbolAsync(resolvedSearchAsset.Symbol, assetType, cancellationToken);

                if (localAsset != null)
                {
                    resolvedAssetId = localAsset.Id;
                    resolvedAssetType = localAsset.Type.Value;
                }
                else
                {
                    willAutoCreate = true;
                }
            }
            else
            {
                isUnresolvable = true;
                warningMessage ??= $"Could not resolve ticker symbol for ISIN '{row.Isin}'.";
            }

            previewRows.Add(new ImportedTransactionRowPreview(
                Raw: row,
                ResolvedSymbol: resolvedSymbol,
                ResolvedAssetId: resolvedAssetId,
                WillAutoCreate: willAutoCreate,
                IsDuplicate: isDuplicate,
                IsUnresolvable: isUnresolvable,
                WarningMessage: warningMessage,
                ResolvedAssetType: resolvedAssetType
            ));
        }

        return previewRows;
    }

    private static AssetType ResolveAssetType(string rawType)
    {
        if (AssetType.TryFromValue(rawType, out var byValue))
        {
            return byValue;
        }

        if (AssetType.TryFromName(rawType, ignoreCase: true, out var byName))
        {
            return byName;
        }

        return AssetType.Stock;
    }
}

