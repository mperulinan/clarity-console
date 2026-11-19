using Portfolio.Application.Models;

namespace Portfolio.Application.Interfaces;

public interface ITransactionImportParser
{
    string BrokerKey { get; }
    string DisplayName { get; }
    Task<ImportParseResult> ParseAsync(
        Stream csvStream, 
        CancellationToken cancellationToken = default);
}
