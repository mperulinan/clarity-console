namespace Portfolio.Application.DTOs;

public record CommitImportTransactionsRequest(
    string PreviewToken,
    IReadOnlyList<string>? ExcludedBrokerTransactionIds = null
);
