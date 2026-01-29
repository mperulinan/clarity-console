using Portfolio.Domain.Entities;

namespace Portfolio.Domain.ValueObjects;

public class ProcessedTransaction
{
    public Transaction Transaction { get; private set; }
    public decimal? ProfitLoss { get; set; }
    public decimal? TotalLossAmount { get; set; }
    public bool IsLossDisallowed { get; set; }
    public int? DisallowedByTransactionId { get; set; }
    public List<int> DisallowsPreviousLosses { get; private set; } = [];

    // Constructor to ensure consistency
    public ProcessedTransaction(Transaction transaction)
    {
        Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
    }
}
