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

    private static readonly Dictionary<string, Guid> _symbolToId = [];
    private static Guid GetId(string symbol)
    {
        if (!_symbolToId.TryGetValue(symbol, out var id))
        {
            id = Guid.NewGuid();
            _symbolToId[symbol] = id;
        }
        return id;
    }

    private static Transaction CreateTx(DateTime date, TransactionType type, string? fromAsset, string? toAsset, decimal spent, decimal received, decimal fromAssetPriceUsd, decimal? fromAssetPriceEur, decimal fee, string? feeAsset, decimal? feeUsdPrice, decimal? feeEurPrice, decimal? xr, string? notes)
    {
        var fromAsstId = fromAsset != null ? GetId(fromAsset) : (Guid?)null;
        var toAsstId = toAsset != null ? GetId(toAsset) : (Guid?)null;
        var feeAsstId = feeAsset != null ? GetId(feeAsset) : (Guid?)null;

        var tx = new Transaction(date, type, fromAsstId, toAsstId, spent, received, fromAssetPriceUsd, fromAssetPriceEur, fee, feeAsstId, feeUsdPrice, feeEurPrice, xr, notes);
        
        if (fromAsset != null)
        {
            tx.FromAsset = new Asset(fromAsset, fromAsset, null, null, fromAsset == "USD" || fromAsset == "EUR" ? AssetType.Fiat : AssetType.Crypto);
        }
        
        if (toAsset != null)
        {
            tx.ToAsset = new Asset(toAsset, toAsset, null, null, toAsset == "USD" || toAsset == "EUR" ? AssetType.Fiat : AssetType.Crypto);
        }

        if (feeAsset != null)
        {
            tx.FeeAsset = new Asset(feeAsset, feeAsset, null, null, feeAsset == "USD" || feeAsset == "EUR" ? AssetType.Fiat : AssetType.Crypto);
        }

        return tx;
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
            CreateTx(new DateTime(2023, 1, 1), TransactionType.Deposit, null, usd, 0, 30000m, 1, 0.9m, 0, null, null, null, null, null),
            // Swap $10k to 1 BTC (Simulating Buy)
            CreateTx(new DateTime(2023, 1, 2), TransactionType.Swap, usd, btc, 10000m, 1, 1, null, 0, null, null, null, null, null),
            // Swap $20k to 1 BTC (Simulating Buy)
            CreateTx(new DateTime(2023, 2, 2), TransactionType.Swap, usd, btc, 20000m, 1, 1, null, 0, null, null, null, null, null),
            // Swap 1.5 BTC to USD at $30k (Simulating Sell)
            CreateTx(new DateTime(2023, 3, 3), TransactionType.Swap, btc, usd, 1.5m, 45000m, 30000m, null, 0, null, null, null, null, null)
        };

        // Act
        var report = _calculator.CalculateInventory(transactions, FiatCurrency.USD);

        var btcId = transactions.First(t => t.ToAsset?.Symbol == btc).ToAssetId!.Value;
        var usdId = transactions.First(t => t.FromAsset?.Symbol == usd).FromAssetId!.Value;

        // Assert
        // First 1 BTC cost $10k. Next 0.5 BTC cost $10k (half of $20k). Total Cost = $20k.
        // Proceeds = 1.5 * $30k = $45k.
        // Profit = $45k - $20k = $25k.

        var sellTx = report.Transactions.Last();
        Assert.Equal(25000m, sellTx.ProfitLoss);
        
        // Remaining inventory: 0.5 BTC @ $20,000 basis = $10,000 total value
        AssetHolding holdingBtc = report.Holdings.Single(h => h.Id == btcId);
        Assert.Equal(0.5m, holdingBtc.Quantity);
        Assert.Equal(20000m, holdingBtc.AvgCost);

        // Remaining cash inventory: 30,000 - 10,000 - 20,000 + 45,000 = 45,000.
        AssetHolding holdingUsd = report.Holdings.Single(h => h.Id == usdId);
        Assert.Equal(45000m, holdingUsd.Quantity);
    }

    [Fact]
    public void CalculateInventory_Reward_ShouldSetProfitInmediately_BasedOnValueAtReceipt()
    {
        // Reward sets profit immediately based on value at receipt.
        string eth = "ETH";

        // Reward: Receive 1 ETH when price is $2000. Fee is 0.
        var rewardTx = CreateTx(
            new DateTime(2023, 1, 1), 
            TransactionType.Reward, 
            null,
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
            CreateTx(new DateTime(2023, 1, 1), TransactionType.Swap, usd, btc, 30000m, 1m, 1m, null, 0, null, null, null, null, null),
            
            // Sell 1 BTC @ 20k (Loss 10k)
            CreateTx(new DateTime(2023, 1, 15), TransactionType.Swap, btc, usd, 1m, 20000m, 20000m, null, 0, null, null, null, null, null),
            
            // Buy 1 BTC @ 22k within 2 months -> Wash Sale!
            CreateTx(new DateTime(2023, 1, 20), TransactionType.Swap, usd, btc, 22000m, 1m, 1m, null, 0, null, null, null, null, null)
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
            CreateTx(new DateTime(2023, 1, 1), TransactionType.Swap, usd, btc, 30000m, 1m, 1m, null, 0, null, null, null, null, null),
            
            // Sell 1 BTC @ 20k (Loss 10k)
            CreateTx(new DateTime(2023, 1, 15), TransactionType.Swap, btc, usd, 1m, 20000m, 20000m, null, 0, null, null, null, null, null),
            
            // Buy 1 BTC @ 22k AFTER 2 months
            CreateTx(new DateTime(2023, 3, 20), TransactionType.Swap, usd, btc, 22000m, 1m, 1m, null, 0, null, null, null, null, null)
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
            CreateTx(new DateTime(2023, 1, 1), TransactionType.Swap, usd, btc, 10000m, 1m, 1m, null, 0, null, null, null, null, null)
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
            // 1. Seed 10k USD (Deposit)
            CreateTx(new DateTime(2023, 1, 1), TransactionType.Deposit, null, usd, 0, 10000m, 1m, 1m, 0, null, null, null, null, null),

            // 2. Buy 1 BTC @ $10,000. Consumes all 10k USD.
            CreateTx(new DateTime(2023, 1, 2), TransactionType.Swap, usd, btc, 10000m, 1m, 1m, null, 0, null, null, null, null, null),
            
            // 3. Sell 2 BTC @ $20,000 each.
            CreateTx(new DateTime(2023, 1, 3), TransactionType.Swap, btc, usd, 2m, 40000m, 20000m, null, 0, null, null, null, null, null)
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
            CreateTx(new DateTime(2023, 1, 1), TransactionType.Swap, usd, eth, 10000m, 10m, 1m, null, 0, null, null, null, null, null),
            
            // Transfer 5 ETH. Fee 0.1 ETH. FeeAsset = ETH.
            CreateTx(new DateTime(2023, 1, 2), TransactionType.Swap, eth, usd, 5m, 5000m, 1000m, null, 0.1m, eth, 1000m, null, null, null)
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
        
        var ethId = transactions.First(t => t.FromAsset?.Symbol == eth).FromAssetId!.Value;
        var holding = report.Holdings.Single(h => h.Id == ethId);
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
            CreateTx(new DateTime(2023, 1, 1), TransactionType.Swap, usd, btc, 10000m, 1m, 1m, null, 0, null, null, null, null, null),
            
            // Sell 0.5 BTC @ 15k (Proceeds 7.5k)
            CreateTx(new DateTime(2023, 1, 2), TransactionType.Swap, btc, usd, 0.5m, 7500m, 15000m, null, 0, null, null, null, null, null)
        };
        
        var btcId = transactions.First(t => t.ToAsset?.Symbol == btc).ToAssetId!.Value;
        var report = _calculator.CalculateInventory(transactions, FiatCurrency.USD);
        var holding = report.Holdings.Single(h => h.Id == btcId);
        
        // Sold 0.5 BTC. Cost basis was 10k * 0.5 = 5k.
        Assert.Equal(5000m, holding.CostBasisOfSold);
        
        // Remaining 0.5 BTC. Cost basis is 5k.
        // Total Invested implicit check = CostBasisOfSold + (Quantity * AvgCost) = 5k + 5k = 10k.
        Assert.Equal(10000m, holding.AvgCost);
        Assert.Equal(0.5m, holding.Quantity);
        
        // Realized PL = 7.5k - 5k = 2.5k.
        Assert.Equal(2500m, holding.RealizedPL);
    }

    [Fact]
    public void CalculateInventory_Withdrawal_ShouldReduceInventory_AndRealizePL()
    {
        // Scenario: Buy 1 BTC @ 10k. Withdraw 0.5 BTC @ 15k market price.
        var btc = "BTC";
        var usd = "USD";
        var transactions = new List<Transaction>
        {
            // 1. Buy 1 BTC @ 10k
            CreateTx(new DateTime(2023, 1, 1), TransactionType.Swap, usd, btc, 10000m, 1m, 1m, null, 0, null, null, null, null, null),
            
            // 2. Withdraw 0.5 BTC. Market price is $15k.
            CreateTx(new DateTime(2023, 1, 2), TransactionType.Withdrawal, btc, null, 0.5m, 0, 15000m, null, 0, null, null, null, null, null)
        };
        
        var btcId = transactions.First(t => t.ToAsset?.Symbol == btc).ToAssetId!.Value;
        var report = _calculator.CalculateInventory(transactions, FiatCurrency.USD);
        var holding = report.Holdings.Single(h => h.Id == btcId);
        
        // 1 BTC @ 10k cost basis. 
        // 0.5 BTC withdrawn. Cost basis of withdrawal = 5k.
        // Market proceeds = 0.5 * 15k = 7.5k.
        // P/L = 7.5k - 5k = 2.5k.
        
        Assert.Equal(0.5m, holding.Quantity);
        Assert.Equal(2500m, holding.RealizedPL);
        Assert.Equal(5000m, holding.CostBasisOfSold);
    }
}
