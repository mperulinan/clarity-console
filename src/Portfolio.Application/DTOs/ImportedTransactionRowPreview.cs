using Portfolio.Application.Models;

namespace Portfolio.Application.DTOs;

public record ImportedTransactionRowPreview(
    ImportedTransactionRow Raw,
    string? ResolvedSymbol,
    Guid? ResolvedAssetId,
    bool WillAutoCreate,
    bool IsDuplicate,
    bool IsUnresolvable,
    string? WarningMessage,
    string? ResolvedAssetType = null
);
