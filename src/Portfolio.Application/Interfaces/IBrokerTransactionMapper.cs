using Portfolio.Application.Models;
using Portfolio.Domain.Entities;

namespace Portfolio.Application.Interfaces;

/// <summary>
/// Strategy interface for mapping raw broker-imported rows into domain Transaction entities.
/// Encapsulates broker-specific conventions (e.g. base currency, leg directions, fee treatment, display naming)
/// so that new brokers (e.g. IBKR) can be added without modifying commit orchestration logic.
/// </summary>
public interface IBrokerTransactionMapper
{
    /// <summary>
    /// Broker key matching the import session broker identifier (e.g. "degiro").
    /// </summary>
    string BrokerKey { get; }

    /// <summary>
    /// Human-readable display name for the broker used in generated notes and summaries (e.g. "DEGIRO").
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Maps a raw imported row and its resolved asset into a domain Transaction entity.
    /// </summary>
    Transaction MapToTransaction(
        ImportedTransactionRow row, 
        Guid targetAssetId);
}
