using Ardalis.SmartEnum;

namespace Portfolio.Domain.Enums;

public class TransactionType : SmartEnum<TransactionType, string>
{
    public static readonly TransactionType Reward     = new("Reward",     "REWARD",     requiresFrom: false, requiresTo: true,  triggersWashSale: false, generatesTaxableIncome: true,  isTaxableDisposal: false, proceedsAreZero: false);
    public static readonly TransactionType Swap       = new("Swap",       "SWAP",       requiresFrom: true,  requiresTo: true,  triggersWashSale: true,  generatesTaxableIncome: false, isTaxableDisposal: true,  proceedsAreZero: false);
    public static readonly TransactionType Deposit    = new("Deposit",    "DEPOSIT",    requiresFrom: false, requiresTo: true,  triggersWashSale: false, generatesTaxableIncome: false, isTaxableDisposal: false, proceedsAreZero: false);
    public static readonly TransactionType Withdrawal = new("Withdrawal", "WITHDRAWAL", requiresFrom: true,  requiresTo: false, triggersWashSale: false, generatesTaxableIncome: false, isTaxableDisposal: false, proceedsAreZero: false);
    public static readonly TransactionType Loss       = new("Loss",       "LOSS",       requiresFrom: true,  requiresTo: false, triggersWashSale: false, generatesTaxableIncome: false, isTaxableDisposal: true,  proceedsAreZero: true);

    /// <summary>
    /// Indicates whether the transaction involves spending or sending an asset out.
    /// </summary>
    public bool RequiresFromAsset { get; }

    /// <summary>
    /// Indicates whether the transaction involves receiving an asset.
    /// </summary>
    public bool RequiresToAsset { get; }

    /// <summary>
    /// Indicates whether this transaction can trigger a wash sale rule.
    /// Typically true for buys (like Swaps) that could offset a recently realized loss.
    /// </summary>
    public bool TriggersWashSale { get; }

    /// <summary>
    /// Indicates whether receiving the asset generates immediate taxable income (e.g., staking rewards or airdrops).
    /// </summary>
    public bool GeneratesTaxableIncome { get; }

    /// <summary>
    /// Indicates whether disposing of the asset is a taxable event (generating capital gains or losses).
    /// For example, Swaps are taxable disposals, but Withdrawals (just moving your assets) are not.
    /// </summary>
    public bool IsTaxableDisposal { get; }

    /// <summary>
    /// Indicates whether disposing of the asset yields zero proceeds (e.g., declaring a total loss of the asset).
    /// </summary>
    public bool ProceedsAreZero { get; }

    /// <summary>
    /// Indicates whether this transaction type represents a taxable event.
    /// Used to determine if associated fees should generate a realized P/L.
    /// Deposits and Withdrawals are not taxable events, so their fees (often fiat transfer fees) don't affect P/L.
    /// </summary>
    public bool IsTaxableEvent => IsTaxableDisposal || GeneratesTaxableIncome;

    /// <summary>
    /// Indicates whether a spot price is strictly required to calculate the transaction's tax implications.
    /// </summary>
    public bool RequiresSpotPrice => (IsTaxableDisposal && !ProceedsAreZero) || GeneratesTaxableIncome;

    private TransactionType(string name, string value, bool requiresFrom, bool requiresTo, bool triggersWashSale, bool generatesTaxableIncome, bool isTaxableDisposal, bool proceedsAreZero) : base(name, value)
    {
        RequiresFromAsset = requiresFrom;
        RequiresToAsset = requiresTo;
        TriggersWashSale = triggersWashSale;
        GeneratesTaxableIncome = generatesTaxableIncome;
        IsTaxableDisposal = isTaxableDisposal;
        ProceedsAreZero = proceedsAreZero;
    }
}
