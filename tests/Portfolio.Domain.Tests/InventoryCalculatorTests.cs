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
    public void CalculateInventory_ShouldFlagError_WhenBuyingWithInsufficientFunds()
    {
        // Scenario: Attempt to Buy 1 BTC with USD, but we have 0 USD.
        var btc = "BTC";
        var usd = "USD";
        var transactions = new List<Transaction>
        {
            // Buy 1 BTC @ $10,000 using USD we don't have.
            new(new DateTime(2023, 1, 1), TransactionTypeEnum.Swap, usd, btc, 10000m, 1m, 1m, null, 0, null, null, null, null, null)
        };

        var report = _calculator.CalculateInventory(transactions, FiatCurrency.USD);
        var buyTx = report.Transactions.First();

        // Expect Error to be set on the BUY transaction for USD
        Assert.NotNull(buyTx.Error);
        Assert.Contains("Insufficient inventory for USD", buyTx.Error);
    }

    [Fact]
    public void CalculateInventory_ShouldFlagError_WhenSellingMoreThanOwned()
    {
        // Scenario: Seed USD. Buy 1 BTC. Sell 2 BTC.        
        var btc = "BTC";
        var usd = "USD";
        var transactions = new List<Transaction>
        {
            // 1. Seed 10k USD (Transfer In)
            new(new DateTime(2023, 1, 1), TransactionTypeEnum.TransferIn, usd, usd, 0, 10000m, 1m, 1m, 0, null, null, null, null, null),

            // 2. Buy 1 BTC @ $10,000. Consumes all 10k USD.
            new(new DateTime(2023, 1, 2), TransactionTypeEnum.Swap, usd, btc, 10000m, 1m, 1m, null, 0, null, null, null, null, null),
            
            // 3. Sell 2 BTC @ $20,000 each.
            new(new DateTime(2023, 1, 3), TransactionTypeEnum.Swap, btc, usd, 2m, 40000m, 20000m, null, 0, null, null, null, null, null)
        };

        var report = _calculator.CalculateInventory(transactions, FiatCurrency.USD);
        var sellTx = report.Transactions.Last();

        // Expect Error to be set on the SELL transaction
        Assert.NotNull(sellTx.Error);
        Assert.Contains("Insufficient inventory for BTC", sellTx.Error);

        // Ensure the BUY transaction did NOT have an error for USD
        Assert.Null(report.Transactions.ElementAt(1).Error);
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
    
    [Fact]
    public void CalculateInventory_ShouldTrackCostBasisOfSold()
    {
        // Scenario: Buy 1 BTC @ 10k. Sell 0.5 BTC @ 15k.
        var btc = "BTC";
        var usd = "USD";
        var transactions = new List<Transaction>
        {
            // Buy 1 BTC @ 10k
            new(new DateTime(2023, 1, 1), TransactionTypeEnum.Swap, usd, btc, 10000m, 1m, 1m, null, 0, null, null, null, null, null),
            
            // Sell 0.5 BTC @ 15k (Proceeds 7.5k)
            new(new DateTime(2023, 1, 2), TransactionTypeEnum.Swap, btc, usd, 0.5m, 7500m, 15000m, null, 0, null, null, null, null, null)
        };
        
        var report = _calculator.CalculateInventory(transactions, FiatCurrency.USD);
        var holding = report.Holdings.Single(h => h.AssetId == btc);
        
        // Sold 0.5 BTC. Cost basis was 10k * 0.5 = 5k.
        Assert.Equal(5000m, holding.CostBasisOfSold);
        
        // Remaining 0.5 BTC. Cost basis is 5k.
        // Total Invested implicit check = CostBasisOfSold + (Quantity * AvgCost) = 5k + 5k = 10k.
        Assert.Equal(10000m, holding.AvgCost);
        Assert.Equal(0.5m, holding.Quantity);
        
        // Realized PL = 7.5k - 5k = 2.5k.
        Assert.Equal(2500m, holding.RealizedPL);
    }
}
