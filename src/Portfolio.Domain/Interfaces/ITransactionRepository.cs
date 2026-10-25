using Portfolio.Domain.Entities;

namespace Portfolio.Domain.Interfaces;

public interface ITransactionRepository
{
    Task<IEnumerable<Transaction>> GetAllAsync();
    Task<Transaction?> GetByIdAsync(int id);
    Task AddAsync(Transaction transaction);
    Task UpdateAsync(Transaction transaction);
    Task DeleteAsync(int id);
    Task<bool> ExistsByBrokerTransactionIdAsync(string brokerTransactionId, CancellationToken cancellationToken = default);
    Task<IReadOnlySet<string>> GetExistingBrokerTransactionIdsAsync(IEnumerable<string> brokerTransactionIds, CancellationToken cancellationToken = default);
}
