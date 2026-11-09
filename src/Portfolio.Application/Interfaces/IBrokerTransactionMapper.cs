using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Application.Interfaces;

/// <summary>
/// Strategy interface for mapping raw broker-imported rows into domain Transaction entities.
/// Encapsulates broker-specific conventions (e.g. base currency, leg directions, fee treatment)
/// so that new brokers (e.g. IBKR) can be added without modifying commit orchestration logic.
/// </summary>
public interface IBrokerTransactionMapper
{
    /// <summary>
    /// Broker key matching the import session broker identifier (e.g. "degiro").
    /// </summary>
    string BrokerKey { get; }

    /// <summary>
    /// Maps a raw imported row and its resolved asset into a domain Transaction entity.
    /// </summary>
    Transaction MapToTransaction(
        ImportedTransactionRow row, 
        Guid targetAssetId, 
        string brokerDisplayName);
}
