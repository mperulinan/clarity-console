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
        // Use the canonical FiatCurrency Guids so Transaction constructor
        // Deposit/Withdrawal validation can recognise them via FiatCurrency.FromIdOrDefault.
        if (symbol == "EUR") return FiatCurrency.EUR.Id;
        if (symbol == "USD") return FiatCurrency.USD.Id;

        if (!_symbolToId.TryGetValue(symbol, out var id))
        {
            id = Guid.NewGuid();
            _symbolToId[symbol] = id;
        }
        return id;
    }

    private static Transaction CreateTx(
        DateTime? date = null,
        TransactionType? type = null,
        string? fromAsset = null,
        string? toAsset = null,
        decimal spent = 0,
        decimal received = 0,
        decimal spotPriceUSD = 0,
        decimal? spotPriceEUR = null,
        decimal fee = 0,
        string? feeAsset = null,
        decimal? feeUsdPrice = null,
        decimal? feeEurPrice = null,
        decimal? xr = null,
        FiatCurrency? spotCurrency = null,
        FiatCurrency? feeCurrency = null,
        string? notes = null)
    {
        var fromAsstId = fromAsset != null ? GetId(fromAsset) : (Guid?)null;
        var toAsstId = toAsset != null ? GetId(toAsset) : (Guid?)null;
        var feeAsstId = feeAsset != null ? GetId(feeAsset) : (Guid?)null;

        var tx = new Transaction(
            date ?? DateTime.UtcNow,
            type ?? TransactionType.Swap,
            fromAsstId,
            toAsstId,
            spent,
            received,
            spotPriceUSD,
            spotPriceEUR,
            fee,
            feeAsstId,
            feeUsdPrice,
            feeEurPrice,
            xr,
            spotCurrency ?? (spotPriceEUR.HasValue && spotPriceUSD == 0 ? FiatCurrency.EUR : FiatCurrency.USD),
            feeCurrency ?? (feeEurPrice.HasValue && !feeUsdPrice.HasValue ? FiatCurrency.EUR : (feeUsdPrice.HasValue ? FiatCurrency.USD : null)),
            notes);

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
        string eur = "EUR";
        var transactions = new List<Transaction>
        {
            // Transfer in 30k EUR (Simulating Deposit)
            CreateTx(date: new DateTime(2023, 1, 1), type: TransactionType.Deposit, toAsset: eur, received: 30000m, spotPriceEUR: 1),
            // Swap 10k EUR to 1 BTC (Simulating Buy)
            CreateTx(date: new DateTime(2023, 1, 2), type: TransactionType.Swap, fromAsset: eur, toAsset: btc, spent: 10000m, received: 1, spotPriceEUR: 1),
            // Swap 20k EUR to 1 BTC (Simulating Buy)
            CreateTx(date: new DateTime(2023, 2, 2), type: TransactionType.Swap, fromAsset: eur, toAsset: btc, spent: 20000m, received: 1, spotPriceEUR: 1),
            // Swap 1.5 BTC to EUR at 30k (Simulating Sell)
            CreateTx(date: new DateTime(2023, 3, 3), type: TransactionType.Swap, fromAsset: btc, toAsset: eur, spent: 1.5m, received: 45000m, spotPriceEUR: 30000m)
        };

        // Act
        var report = _calculator.CalculateInventory(transactions, FiatCurrency.EUR);

        var btcId = transactions.First(t => t.ToAsset?.Symbol == btc).ToAssetId!.Value;
        var eurId = transactions.First(t => t.FromAsset?.Symbol == eur).FromAssetId!.Value;

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
        AssetHolding holdingEur = report.Holdings.Single(h => h.Id == eurId);
        Assert.Equal(45000m, holdingEur.Quantity);
    }

    [Fact]
    public void CalculateInventory_Reward_ShouldSetProfitInmediately_BasedOnValueAtReceipt()
    {
        // Reward sets profit immediately based on value at receipt.
        string eth = "ETH";

        // Reward: Receive 1 ETH when price is $2000. Fee is 0.
        var rewardTx = CreateTx(
            date: new DateTime(2023, 1, 1),
            type: TransactionType.Reward,
            toAsset: eth,
            received: 1m,
            spotPriceUSD: 2000m
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
            CreateTx(date: new DateTime(2023, 1, 1), fromAsset: usd, toAsset: btc, spent: 30000m, received: 1m, spotPriceUSD: 1m),
            
            // Sell 1 BTC @ 20k (Loss 10k)
            CreateTx(date: new DateTime(2023, 1, 15), fromAsset: btc, toAsset: usd, spent: 1m, received: 20000m, spotPriceUSD: 20000m),
            
            // Buy 1 BTC @ 22k within 2 months -> Wash Sale!
            CreateTx(date: new DateTime(2023, 1, 20), fromAsset: usd, toAsset: btc, spent: 22000m, received: 1m, spotPriceUSD: 1m)
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
            CreateTx(date: new DateTime(2023, 1, 1), fromAsset: usd, toAsset: btc, spent: 30000m, received: 1m, spotPriceUSD: 1m),
            
            // Sell 1 BTC @ 20k (Loss 10k)
            CreateTx(date: new DateTime(2023, 1, 15), fromAsset: btc, toAsset: usd, spent: 1m, received: 20000m, spotPriceUSD: 20000m),
            
            // Buy 1 BTC @ 22k AFTER 2 months
            CreateTx(date: new DateTime(2023, 3, 20), fromAsset: usd, toAsset: btc, spent: 22000m, received: 1m, spotPriceUSD: 1m)
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
    public void CalculateInventory_WashSale_ShouldNotDisallowLoss_WhenBuyIsReward()
    {
        var btc = "BTC";
        var usd = "USD";
        var transactions = new List<Transaction>
        {
            // Buy 1 BTC @ 30k
            CreateTx(date: new DateTime(2023, 1, 1), fromAsset: usd, toAsset: btc, spent: 30000m, received: 1m, spotPriceUSD: 1m),
            
            // Sell 1 BTC @ 20k (Loss 10k)
            CreateTx(date: new DateTime(2023, 1, 15), fromAsset: btc, toAsset: usd, spent: 1m, received: 20000m, spotPriceUSD: 20000m),
            
            // Receive 1 BTC Reward within 2 months
            CreateTx(date: new DateTime(2023, 1, 20), type: TransactionType.Reward, toAsset: btc, received: 1m, spotPriceUSD: 22000m)
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
    public void CalculateInventory_WashSale_ShouldNotDisallowLoss_WhenTransactionIsLoss()
    {
        var btc = "BTC";
        var transactions = new List<Transaction>
        {
            // Deposit EUR
            CreateTx(date: new DateTime(2022, 12, 31), type: TransactionType.Deposit, toAsset: FiatCurrency.EUR.Symbol, received: 30000m, spotPriceUSD: 1m),

            // Buy 1 BTC @ 30k
            CreateTx(date: new DateTime(2023, 1, 1), fromAsset: FiatCurrency.EUR.Symbol, toAsset: btc, spent: 30000m, received: 1m, spotPriceUSD: 1m),
            
            // Lose 1 BTC (Total Loss of cost basis 30k)
            CreateTx(date: new DateTime(2023, 1, 15), type: TransactionType.Loss, fromAsset: btc, spent: 1m, spotPriceUSD: 20000m),
            
            // Deposit more EUR and swap them for USD
            CreateTx(date: new DateTime(2023, 1, 17), type: TransactionType.Deposit, toAsset: FiatCurrency.EUR.Symbol, received: 30000m, spotPriceUSD: 1m),
            CreateTx(date: new DateTime(2023, 1, 18), type: TransactionType.Swap, fromAsset: FiatCurrency.EUR.Symbol, toAsset: FiatCurrency.USD.Symbol, spent: 30000m, received: 33000m, spotPriceUSD: 1.1m),
            
            // Buy 1 BTC @ 22k within 2 months
            CreateTx(date: new DateTime(2023, 1, 20), fromAsset: FiatCurrency.USD.Symbol, toAsset: btc, spent: 22000m, received: 1m, spotPriceUSD: 1m)
        };

        var report = _calculator.CalculateInventory(transactions, FiatCurrency.USD);
        var reportTransactions = report.Transactions.ToList();

        // Verify Loss is ALLOWED
        var lossTx = reportTransactions[2];
        Assert.False(lossTx.IsLossDisallowed);
        Assert.Equal(-30000m, lossTx.ProfitLoss);

        // Ensure no disallowance link
        Assert.Empty(reportTransactions[5].DisallowsPreviousLosses);
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
            CreateTx(date: new DateTime(2023, 1, 1), fromAsset: usd, toAsset: btc, spent: 10000m, received: 1m, spotPriceUSD: 1m)
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
        // Scenario: Seed EUR. Buy 1 BTC. Sell 2 BTC.        
        var btc = "BTC";
        var eur = "EUR";
        var transactions = new List<Transaction>
        {
            // 1. Seed 10k EUR (Deposit)
            CreateTx(date: new DateTime(2023, 1, 1), type: TransactionType.Deposit, toAsset: eur, received: 10000m, spotPriceEUR: 1m),

            // 2. Buy 1 BTC @ 10,000 EUR. Consumes all 10k EUR.
            CreateTx(date: new DateTime(2023, 1, 2), fromAsset: eur, toAsset: btc, spent: 10000m, received: 1m, spotPriceEUR: 1m),
            
            // 3. Sell 2 BTC @ 20,000 EUR each.
            CreateTx(date: new DateTime(2023, 1, 3), fromAsset: btc, toAsset: eur, spent: 2m, received: 40000m, spotPriceEUR: 20000m)
        };

        var report = _calculator.CalculateInventory(transactions, FiatCurrency.EUR);
        var sellTx = report.Transactions.Last();

        // Expect Error to be set on the SELL transaction
        Assert.NotNull(sellTx.Error);
        Assert.Contains("Insufficient inventory for BTC", sellTx.Error);

        // Ensure the BUY transaction did NOT have an error for EUR
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
            CreateTx(date: new DateTime(2023, 1, 1), fromAsset: usd, toAsset: eth, spent: 10000m, received: 10m, spotPriceUSD: 1m),
            
            // Transfer 5 ETH. Fee 0.1 ETH. FeeAsset = ETH.
            CreateTx(date: new DateTime(2023, 1, 2), fromAsset: eth, toAsset: usd, spent: 5m, received: 5000m, spotPriceUSD: 1000m, fee: 0.1m, feeAsset: eth, feeUsdPrice: 1000m)
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
            CreateTx(date: new DateTime(2023, 1, 1), fromAsset: usd, toAsset: btc, spent: 10000m, received: 1m, spotPriceUSD: 1m),
            
            // Sell 0.5 BTC @ 15k (Proceeds 7.5k)
            CreateTx(date: new DateTime(2023, 1, 2), fromAsset: btc, toAsset: usd, spent: 0.5m, received: 7500m, spotPriceUSD: 15000m)
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
    public void CalculateInventory_Withdrawal_ShouldReduceInventory()
    {
        // Scenario: Deposit 10k EUR. Withdraw 5k EUR.
        var eur = "EUR";
        var transactions = new List<Transaction>
        {
            // 1. Deposit 10k EUR
            CreateTx(date: new DateTime(2023, 1, 1), type: TransactionType.Deposit, toAsset: eur, received: 10000m, spotPriceEUR: 1m),
            
            // 2. Withdraw 5k EUR.
            CreateTx(date: new DateTime(2023, 1, 2), type: TransactionType.Withdrawal, fromAsset: eur, spent: 5000m, spotPriceEUR: 1m)
        };

        var eurId = transactions.First(t => t.ToAsset?.Symbol == eur).ToAssetId!.Value;
        var report = _calculator.CalculateInventory(transactions, FiatCurrency.EUR);
        var holding = report.Holdings.Single(h => h.Id == eurId);

        // 10k EUR deposited. 
        // 5k EUR withdrawn.

        Assert.Equal(5000m, holding.Quantity);
        Assert.Equal(0m, holding.RealizedPL);
        Assert.Equal(5000m, holding.CostBasisOfSold);
    }

    [Fact]
    public void CalculateInventory_Loss_ShouldRealizeNegativePL_Equal_To_CostBasis()
    {
        var eur = "EUR";
        var btc = "BTC";
        var transactions = new List<Transaction>
        {
            CreateTx(date: new DateTime(2023, 1, 1), type: TransactionType.Deposit, toAsset: eur, received: 10000m, spotPriceEUR: 1m),
            CreateTx(date: new DateTime(2023, 1, 2), type: TransactionType.Swap, fromAsset: eur, toAsset: btc, spent: 5000m, received: 1m, spotPriceEUR: 1m),
            // Loss of 0.5 BTC. Spot price doesn't matter for proceeds (ProceedsAreZero = true), but let's say it's 6000 EUR
            CreateTx(date: new DateTime(2023, 1, 3), type: TransactionType.Loss, fromAsset: btc, spent: 0.5m, spotPriceEUR: 6000m)
        };

        var report = _calculator.CalculateInventory(transactions, FiatCurrency.EUR);

        Assert.DoesNotContain(report.Transactions, t => t.Error != null);

        var btcId = transactions.First(t => t.ToAsset?.Symbol == btc).ToAssetId!.Value;
        var btcHolding = report.Holdings.SingleOrDefault(h => h.Id == btcId);

        Assert.NotNull(btcHolding);
        Assert.Equal(0.5m, btcHolding.Quantity);

        // Cost basis of the 0.5 lost BTC is 2500 EUR. Proceeds are 0. PL is -2500.
        Assert.Equal(-2500m, btcHolding.RealizedPL);

        var lossTx = report.Transactions.Single(t => t.Transaction.Type == TransactionType.Loss);
        Assert.Equal(-2500m, lossTx.ProfitLoss);
    }

    [Fact]
    public void CalculateInventory_Loss_ShouldRealizeNegativePL_EvenWithoutSpotPrice()
    {
        // Spot price is NOT required for a Loss (RequiresSpotPrice = false).
        // PL should equal the negative cost basis regardless.
        var eur = "EUR";
        var btc = "BTC";
        var transactions = new List<Transaction>
        {
            CreateTx(date: new DateTime(2023, 1, 1), type: TransactionType.Deposit, toAsset: eur, received: 10000m, spotPriceEUR: 1m),
            CreateTx(date: new DateTime(2023, 1, 2), type: TransactionType.Swap, fromAsset: eur, toAsset: btc, spent: 5000m, received: 1m, spotPriceEUR: 1m),
            // Loss of 0.5 BTC with NO spot price provided.
            CreateTx(date: new DateTime(2023, 1, 3), type: TransactionType.Loss, fromAsset: btc, spent: 0.5m)
        };

        var report = _calculator.CalculateInventory(transactions, FiatCurrency.EUR);

        Assert.DoesNotContain(report.Transactions, t => t.Error != null);

        var lossTx = report.Transactions.Single(t => t.Transaction.Type == TransactionType.Loss);

        // Cost basis of the 0.5 lost BTC is 2500 EUR. Proceeds are 0. PL is -2500.
        Assert.Equal(-2500m, lossTx.ProfitLoss);
    }

    [Fact]
    public void CalculateInventory_Loss_ShouldIncludeFeeInNegativePL()
    {
        var eur = FiatCurrency.EUR.Symbol;
        var btc = "BTC";
        var transactions = new List<Transaction>
        {
            // 1. Deposit 10100 EUR.
            CreateTx(date: new DateTime(2023, 1, 1), type: TransactionType.Deposit, toAsset: eur, received: 10100m, spotPriceEUR: 1m),

            // 2. Buy 1 BTC for 10000 EUR. Cost basis = 10000 EUR / BTC
            CreateTx(date: new DateTime(2023, 1, 2), type: TransactionType.Swap, fromAsset: eur, toAsset: btc, spent: 10000m, received: 1m, spotPriceEUR: 1m),
            
            // 3. Lose 0.5 BTC, and pay a fee of 100 EUR in the process.
            // The cost basis of 0.5 BTC is 5000 EUR. 
            // The total loss should be the lost asset's cost basis (5000) + the fee (100) = 5100 EUR loss.
            CreateTx(date: new DateTime(2023, 1, 3), type: TransactionType.Loss, fromAsset: btc, spent: 0.5m, spotPriceEUR: 10000m, fee: 100m, feeAsset: eur, feeEurPrice: 1m)
        };

        var report = _calculator.CalculateInventory(transactions, FiatCurrency.EUR);

        Assert.DoesNotContain(report.Transactions, t => t.Error != null);

        var lossTx = report.Transactions.Single(t => t.Transaction.Type == TransactionType.Loss);
        
        // -5000 (Lost Asset Cost Basis) - 100 (Fee) = -5100 EUR Total PL
        Assert.Equal(-5100m, lossTx.ProfitLoss); 
    }

    [Fact]
    public void CalculateInventory_Reward_ShouldIncludeFeeInNetPL()
    {
        var eur = FiatCurrency.EUR.Symbol;
        var btc = "BTC";
        var transactions = new List<Transaction>
        {
            // 1. Deposit 150 EUR.
            CreateTx(date: new DateTime(2023, 1, 1), type: TransactionType.Deposit, toAsset: eur, received: 150m, spotPriceEUR: 1m),
            
            // 2. Receive Reward of 1 BTC (worth 1000 EUR), and pay a fee of 150 EUR.
            // Income = 1000 EUR. 
            // Fee Expense = 150 EUR (Capital gain on EUR is 0). Net Fee Impact = -150.
            // Total P/L = 1000 - 150 = 850 EUR.
            CreateTx(date: new DateTime(2023, 1, 2), type: TransactionType.Reward, toAsset: btc, received: 1m, spotPriceEUR: 1000m, fee: 150m, feeAsset: eur, feeEurPrice: 1m)
        };

        var report = _calculator.CalculateInventory(transactions, FiatCurrency.EUR);

        Assert.DoesNotContain(report.Transactions, t => t.Error != null);

        var rewardTx = report.Transactions.Single(t => t.Transaction.Type == TransactionType.Reward);
        
        Assert.Equal(850m, rewardTx.ProfitLoss); 
    }
}
