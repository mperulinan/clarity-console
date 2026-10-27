namespace Portfolio.Application.DTOs;

public record ImportTransactionsPreviewDto(
    string PreviewToken,
    IReadOnlyList<ImportedTransactionRowPreview> Rows,
    int TotalRows,
    int DuplicateRows,
    int UnresolvableRows,
    int ReadyToImportRows
);
