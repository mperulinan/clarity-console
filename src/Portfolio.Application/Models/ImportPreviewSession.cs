using Portfolio.Application.DTOs;

namespace Portfolio.Application.Models;

public record ImportPreviewSession(
    string BrokerKey,
    IReadOnlyList<ImportedTransactionRowPreview> Rows,
    DateTimeOffset CreatedAt
);
