using CsvHelper;

namespace Portfolio.Infrastructure.ImportParsers;

/// <summary>
/// Resolves the DEGIRO order transaction ID across multiple CSV export formats and column alignment variations.
/// </summary>
internal static class DegiroOrderIdResolver
{
    /// <summary>
    /// Resolves the broker transaction ID using a 4-step heuristic:
    /// 1. Direct "ID Orden" header column.
    /// 2. Adjacent shifted column (legacy exports had an unheadered column pushing values one to the right).
    /// 3. Right-to-left scan of the row for a GUID or order token format.
    /// 4. Deterministic fallback ID based on date, ISIN, quantity, and total EUR.
    /// </summary>
    public static string Resolve(
        CsvReader csv, 
        string[]? headers, 
        DateTime dateTime, 
        string isin, 
        decimal quantity, 
        decimal totalEur)
    {
        // 1. Direct header lookup
        var id = csv.GetField("ID Orden")?.Trim();
        if (!string.IsNullOrWhiteSpace(id))
            return id;

        // 2. Adjacent shifted column check
        if (headers != null)
        {
            var idIndex = Array.FindIndex(headers, h => string.Equals(h?.Trim(), "ID Orden", StringComparison.OrdinalIgnoreCase));
            if (idIndex >= 0)
            {
                var adjacent = GetFieldByIndexSafe(csv, idIndex + 1);
                if (!string.IsNullOrWhiteSpace(adjacent))
                    return adjacent;
            }
        }

        // 3. Scan row from right to left for a GUID or order-like token
        for (int col = csv.ColumnCount - 1; col >= 0; col--)
        {
            var val = GetFieldByIndexSafe(csv, col);
            if (!string.IsNullOrWhiteSpace(val))
            {
                if (Guid.TryParse(val, out _) || (val.Length > 8 && val.Contains('-')))
                    return val;
            }
        }

        // 4. Deterministic fallback if broker didn't provide an order ID
        return $"DEGIRO_{dateTime:yyyyMMddHHmm}_{isin}_{quantity}_{totalEur}".Replace(' ', '_');
    }

    public static string? GetFieldByIndexSafe(CsvReader csv, int index)
    {
        try
        {
            return csv.GetField(index)?.Trim();
        }
        catch
        {
            return null;
        }
    }
}

