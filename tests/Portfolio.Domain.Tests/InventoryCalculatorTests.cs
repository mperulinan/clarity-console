using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Services;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Tests;

public class InventoryCalculatorTests
{
    private readonly InventoryCalculator _calculator;

    public InventoryCalculatorTests()
    {
        _calculator = new InventoryCalculator();
    }

    [Fact]
    public void CalculateInventory_Fifo_ShouldConsumeOldestInventoryFirst()
    {
        // Arrange
        string btc = "BTC";
        string usd = "USD";
        var transactions = new List<Transaction>
        {
            // Transfer in 30k USD (Simulating Deposit)
            new(new DateTime(2023, 1, 1), TransactionTypeEnum.TransferIn, usd, usd, 0, 30000m, 1, 0.9m, 0, null, null, null, null, null),
            // Swap $10k to 1 BTC (Simulating Buy)
            new(new DateTime(2023, 1, 2), TransactionTypeEnum.Swap, usd, btc, 10000m, 1, 1, null, 0, null, null, null, null, null),
            // Swap $20k to 1 BTC (Simulating Buy)
            new(new DateTime(2023, 2, 2), TransactionTypeEnum.Swap, usd, btc, 20000m, 1, 1, null, 0, null, null, null, null, null),
            // Swap 1.5 BTC to USD at $30k (Simulating Sell)
            new(new DateTime(2023, 3, 3), TransactionTypeEnum.Swap, btc, usd, 1.5m, 45000m, 30000m, null, 0, null, null, null, null, null)
        };

        // Act
        var report = _calculator.CalculateInventory(transactions, FiatCurrency.USD);

        // Assert
        // First 1 BTC cost $10k. Next 0.5 BTC cost $10k (half of $20k). Total Cost = $20k.
        // Proceeds = 1.5 * $30k = $45k.
        // Profit = $45k - $20k = $25k.

        var sellTx = report.Transactions.Last();
        Assert.Equal(25000m, sellTx.ProfitLoss);
        
        // Remaining inventory: 0.5 BTC @ $20,000 basis = $10,000 total value
        AssetHolding holdingBtc = report.Holdings.Single(h => h.AssetId == btc);
        Assert.Equal(0.5m, holdingBtc.Quantity);
        Assert.Equal(20000m, holdingBtc.AvgCost);

        // Remaining cash inventory: 30,000 - 10,000 - 20,000 + 45,000 = 45,000.
        AssetHolding holdingUsd = report.Holdings.Single(h => h.AssetId == usd);
        Assert.Equal(45000m, holdingUsd.Quantity);
    }

    [Fact]
    public void CalculateInventory_Reward_ShouldSetProfitInmediately_BasedOnValueAtReceipt()
    {
        // Reward sets profit immediately based on value at receipt.
        string eth = "ETH";

        // Reward: Receive 1 ETH when price is $2000. Fee is 0.
        var rewardTx = new Transaction(
            new DateTime(2023, 1, 1), 
            TransactionTypeEnum.Reward, 
            eth,
            eth, 
            0m,
            1m, // AmountReceived
            2000m, // FromAssetPriceInUsd
            null, 0, null, null, null, null, null
        );

        var txList = new List<Transaction> { rewardTx };

        var report = _calculator.CalculateInventory(txList, FiatCurrency.USD);

        var pt = report.Transactions.First();
        // Reward is recognized as income = 1 * 2000 = 2000.
        Assert.Equal(2000m, pt.ProfitLoss);

        // And it should be added to inventory at cost basis $2000.
        AssetHolding holding = report.Holdings.Single();
        Assert.Equal(1m, holding.Quantity);
        Assert.Equal(2000m, holding.AvgCost);
    }

    [Fact]
    public void CalculateInventory_WashSale_ShouldDisallowLoss_WhenBuyOccursWithin2Months()
    {
        string btc = "BTC";
        string usd = "USD";
        var transactions = new List<Transaction>
        {
            // Buy 1 BTC @ 30k
            new(new DateTime(2023, 1, 1), TransactionTypeEnum.Swap, usd, btc, 30000m, 1m, 1m, null, 0, null, null, null, null, null),
            
            // Sell 1 BTC @ 20k (Loss 10k)
            new(new DateTime(2023, 1, 15), TransactionTypeEnum.Swap, btc, usd, 1m, 20000m, 20000m, null, 0, null, null, null, null, null),
            
            // Buy 1 BTC @ 22k within 2 months -> Wash Sale!
            new(new DateTime(2023, 1, 20), TransactionTypeEnum.Swap, usd, btc, 22000m, 1m, 1m, null, 0, null, null, null, null, null)
        };

        var report = _calculator.CalculateInventory(transactions, FiatCurrency.USD);
        var reportTransactions = report.Transactions.ToList();

        // Verify Loss is disallowed
        var sellTx = reportTransactions[1];
        Assert.True(sellTx.IsLossDisallowed);
        Assert.Equal(-10000m, sellTx.ProfitLoss);

        var buyTx = reportTransactions[2];
        Assert.NotNull(buyTx.DisallowsPreviousLosses);
        Assert.Contains(sellTx.Transaction.Id, buyTx.DisallowsPreviousLosses);
    }

    [Fact]
    public void CalculateInventory_WashSale_ShouldNotDisallowLoss_WhenBuyOccursAfterWindow()
    {
        var btc = "BTC";
        var usd = "USD";
        var transactions = new List<Transaction>
        {
            // Buy 1 BTC @ 30k
            new(new DateTime(2023, 1, 1), TransactionTypeEnum.Swap, usd, btc, 30000m, 1m, 1m, null, 0, null, null, null, null, null),
            
            // Sell 1 BTC @ 20k (Loss 10k)
            new(new DateTime(2023, 1, 15), TransactionTypeEnum.Swap, btc, usd, 1m, 20000m, 20000m, null, 0, null, null, null, null, null),
            
            // Buy 1 BTC @ 22k AFTER 2 months
            new(new DateTime(2023, 3, 20), TransactionTypeEnum.Swap, usd, btc, 22000m, 1m, 1m, null, 0, null, null, null, null, null)
        };

        var report = _calculator.CalculateInventory(transactions, FiatCurrency.USD);
        var reportTransactions = report.Transactions.ToList();

        // Verify Loss is ALLOWED
        var sellTx = reportTransactions[1];
        Assert.False(sellTx.IsLossDisallowed);
        Assert.Equal(-10000m, sellTx.ProfitLoss);
                
        // Ensure no disallowance link
        Assert.Empty(reportTransactions[2].DisallowsPreviousLosses);
    }

    [Fact]
    public void CalculateInventory_Fee_ShouldReduceInventory_WhenFeePaidInAsset()
    {
        // Scenario: Buy 10 ETH. Then Swap 5 ETH for USD @ $10k, paying 0.1 ETH fee.
        // Fee comes out of inventory.
        var eth = "ETH";
        var usd = "USD";
        var transactions = new List<Transaction>
        {
            // Buy 10 ETH @ $1000 = $10k.
            new(new DateTime(2023, 1, 1), TransactionTypeEnum.Swap, usd, eth, 10000m, 10m, 1m, null, 0, null, null, null, null, null),
            
            // Transfer 5 ETH. Fee 0.1 ETH. FeeAsset = ETH.
            new(new DateTime(2023, 1, 2), TransactionTypeEnum.Swap, eth, usd, 5m, 5000m, 1000m, null, 0.1m, eth, 1000m, null, null, null)
        };
        
        var report = _calculator.CalculateInventory(transactions, FiatCurrency.USD);
        var reportTransactions = report.Transactions.ToList();
        
        var buyTx = reportTransactions[0];
        
        // Fee PL: Consumed 0.1 ETH. Cost Basis $100. (0.1 * 1000).
        // Fee Value at time of spend: 0.1 * 1000 = $100.
        // Fee PL = 100 - 100 = 0.
        
        // Inventory remaining:
        // Initial: 10.
        // Spent for swap: 5.
        // Spent for fee: 0.1.
        // Remaining: 4.9.
        
        var holding = report.Holdings.Single(h => h.AssetId == eth);
        Assert.Equal(4.9m, holding.Quantity);
    }
}
