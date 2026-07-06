using Portfolio.Domain.Enums;
using Xunit;

namespace Portfolio.Domain.Tests;

public class TransactionTypeTests
{
    [Fact]
    public void Reward_Properties_Are_Correctly_Set()
    {
        var type = TransactionType.Reward;
        Assert.False(type.RequiresFromAsset);
        Assert.True(type.RequiresToAsset);
        Assert.False(type.TriggersWashSale);
        Assert.True(type.GeneratesTaxableIncome);
        Assert.False(type.IsTaxableDisposal);
        Assert.False(type.ProceedsAreZero);
        Assert.True(type.RequiresSpotPrice);
    }

    [Fact]
    public void Swap_Properties_Are_Correctly_Set()
    {
        var type = TransactionType.Swap;
        Assert.True(type.RequiresFromAsset);
        Assert.True(type.RequiresToAsset);
        Assert.True(type.TriggersWashSale);
        Assert.False(type.GeneratesTaxableIncome);
        Assert.True(type.IsTaxableDisposal);
        Assert.False(type.ProceedsAreZero);
        Assert.True(type.RequiresSpotPrice);
    }

    [Fact]
    public void Deposit_Properties_Are_Correctly_Set()
    {
        var type = TransactionType.Deposit;
        Assert.False(type.RequiresFromAsset);
        Assert.True(type.RequiresToAsset);
        Assert.False(type.TriggersWashSale);
        Assert.False(type.GeneratesTaxableIncome);
        Assert.False(type.IsTaxableDisposal);
        Assert.False(type.ProceedsAreZero);
        Assert.False(type.RequiresSpotPrice);
    }

    [Fact]
    public void Withdrawal_Properties_Are_Correctly_Set()
    {
        var type = TransactionType.Withdrawal;
        Assert.True(type.RequiresFromAsset);
        Assert.False(type.RequiresToAsset);
        Assert.False(type.TriggersWashSale);
        Assert.False(type.GeneratesTaxableIncome);
        Assert.False(type.IsTaxableDisposal);
        Assert.False(type.ProceedsAreZero);
        Assert.False(type.RequiresSpotPrice);
    }

    [Fact]
    public void Loss_Properties_Are_Correctly_Set()
    {
        var type = TransactionType.Loss;
        Assert.True(type.RequiresFromAsset);
        Assert.False(type.RequiresToAsset);
        Assert.False(type.TriggersWashSale);
        Assert.False(type.GeneratesTaxableIncome);
        Assert.True(type.IsTaxableDisposal);
        Assert.True(type.ProceedsAreZero);
        Assert.False(type.RequiresSpotPrice);
    }
}
