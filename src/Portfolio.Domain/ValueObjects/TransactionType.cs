using Ardalis.SmartEnum;

namespace Portfolio.Domain.ValueObjects;

public class TransactionType : SmartEnum<TransactionType, string>
{
    public static readonly TransactionType Reward = new("Reward", "REWARD");
    public static readonly TransactionType Swap = new("Swap", "SWAP");
    public static readonly TransactionType TransferIn = new("Transfer In", "TRANSFER_IN");

    private TransactionType(string name, string value) : base(name, value)
    {
    }
}
