using Ardalis.SmartEnum;

namespace Portfolio.Domain.Enums
{
    public class TransactionTypeEnum : SmartEnum<TransactionTypeEnum, string>
    {
        public static readonly TransactionTypeEnum Reward = new("Reward", "REWARD");
        public static readonly TransactionTypeEnum Swap = new("Swap", "SWAP");
        public static readonly TransactionTypeEnum TransferIn = new("Transfer In", "TRANSFER_IN");

        private TransactionTypeEnum(string name, string value) : base(name, value)
        {
        }
    }
}
