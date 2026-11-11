using Microsoft.Extensions.Logging;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using Portfolio.Application.Models;

namespace Portfolio.Application.Services;

/// <summary>
/// Resolves ISIN codes to external asset data via <see cref="IAssetSearchService"/>.
///
/// Resolution strategy per ISIN:
///   1. Search by ISIN.
///   2. If no results, fall back to searching by the row's ProductName.
///   3. Among candidates, prefer those with existing local transactions, then those whose
///      name contains the row's reference exchange string.
///   4. On search failure, logs a warning and maps the ISIN to null.
/// </summary>
public class ImportTickerResolver(
    IAssetSearchService searchService,
    ILogger<ImportTickerResolver> logger) : IImportTickerResolver
{
    public async Task<IReadOnlyDictionary<string, AssetDto?>> ResolveIsinsAsync(
        IEnumerable<ImportedTransactionRow> rows,
        CancellationToken cancellationToken = default)
    {
        // Group by distinct ISIN to minimize external searches and preserve row context
        var distinctIsins = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.Isin))
            .GroupBy(r => r.Isin, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var isinMap = new Dictionary<string, AssetDto?>(StringComparer.OrdinalIgnoreCase);

        foreach (var (isin, sampleRow) in distinctIsins)
        {
            try
            {
                // Search without type filter so both Stocks and ETFs are resolved
                var searchResults = (await searchService.SearchAsync(isin)).ToList();

                // Fallback: if ISIN lookup returned 0 results, attempt search with the product name
                if (searchResults.Count == 0 && !string.IsNullOrWhiteSpace(sampleRow.ProductName))
                {
                    searchResults = (await searchService.SearchAsync(sampleRow.ProductName)).ToList();
                }

                if (searchResults.Count > 0)
                {
                    // Prioritization heuristic:
                    // 1. Prefer candidate already existing locally with transactions
                    // 2. Prefer candidate matching reference exchange if available
                    // 3. Fall back to highest relevance result
                    var match = searchResults
                        .OrderByDescending(c => c.TransactionCount > 0)
                        .ThenByDescending(c => !string.IsNullOrWhiteSpace(sampleRow.Exchange) &&
                                               c.Name.Contains(sampleRow.Exchange, StringComparison.OrdinalIgnoreCase))
                        .FirstOrDefault();

                    isinMap[isin] = match;
                }
                else
                {
                    isinMap[isin] = null;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to search for asset with ISIN '{Isin}'", isin);
                isinMap[isin] = null;
            }
        }

        return isinMap;
    }
}

