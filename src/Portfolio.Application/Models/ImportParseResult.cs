namespace Portfolio.Application.Models;

/// <summary>
/// Result of parsing an imported broker CSV or export file.
/// Contains the successfully extracted transaction rows along with
/// observability metadata regarding any skipped or unparseable rows.
/// </summary>
public record ImportParseResult(
    IReadOnlyList<ImportedTransactionRow> Rows,
    int SkippedRowCount = 0,
    IReadOnlyList<string>? Warnings = null
)
{
    public IReadOnlyList<string> Warnings { get; init; } = Warnings ?? [];

    public static ImportParseResult Empty { get; } = new([], 0, []);
}
