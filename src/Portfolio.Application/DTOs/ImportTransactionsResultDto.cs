namespace Portfolio.Application.DTOs;

public record ImportTransactionsResultDto(
    int ImportedCount,
    int SkippedCount,
    IReadOnlyList<string> Warnings
);
