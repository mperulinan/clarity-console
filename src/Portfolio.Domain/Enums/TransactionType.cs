using Ardalis.SmartEnum;

namespace Portfolio.Domain.Enums;

public class TransactionType : SmartEnum<TransactionType, string>
{
    public static readonly TransactionType Reward = new("Reward", "REWARD", requiresFrom: false, requiresTo: true, triggersWashSale: false);
    public static readonly TransactionType Swap = new("Swap", "SWAP", requiresFrom: true, requiresTo: true, triggersWashSale: true);
    public static readonly TransactionType Deposit = new("Deposit", "DEPOSIT", requiresFrom: false, requiresTo: true, triggersWashSale: false);
    public static readonly TransactionType Withdrawal = new("Withdrawal", "WITHDRAWAL", requiresFrom: true, requiresTo: false, triggersWashSale: false);

    public bool RequiresFromAsset { get; }
    public bool RequiresToAsset { get; }
    public bool TriggersWashSale { get; }

    private TransactionType(string name, string value, bool requiresFrom, bool requiresTo, bool triggersWashSale) : base(name, value)
    {
        RequiresFromAsset = requiresFrom;
        RequiresToAsset = requiresTo;
        TriggersWashSale = triggersWashSale;
    }
}
