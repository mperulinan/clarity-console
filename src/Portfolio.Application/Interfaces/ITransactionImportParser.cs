using Portfolio.Application.Models;

namespace Portfolio.Application.Interfaces;

public interface ITransactionImportParser
{
    string BrokerKey { get; }
    string DisplayName { get; }
    Task<IReadOnlyList<ImportedTransactionRow>> ParseAsync(
        Stream csvStream, 
        CancellationToken cancellationToken = default);
}
