using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Interfaces;

public interface ITransactionImportParser
{
    string BrokerKey { get; }
    Task<IReadOnlyList<ImportedTransactionRow>> ParseAsync(
        Stream csvStream, 
        CancellationToken cancellationToken = default);
}
