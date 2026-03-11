using Ardalis.SmartEnum;

namespace Portfolio.Domain.ValueObjects;

public class TransactionType : SmartEnum<TransactionType, string>
{
    public static readonly TransactionType Reward = new("Reward", "REWARD", requiresFrom: false, requiresTo: true);
    public static readonly TransactionType Swap = new("Swap", "SWAP", requiresFrom: true, requiresTo: true);
    public static readonly TransactionType Deposit = new("Deposit", "DEPOSIT", requiresFrom: false, requiresTo: true);
    public static readonly TransactionType Withdrawal = new("Withdrawal", "WITHDRAWAL", requiresFrom: true, requiresTo: false);

    public bool RequiresFromAsset { get; }
    public bool RequiresToAsset { get; }

    private TransactionType(string name, string value, bool requiresFrom, bool requiresTo) : base(name, value)
    {
        RequiresFromAsset = requiresFrom;
        RequiresToAsset = requiresTo;
    }
}
