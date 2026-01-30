using Portfolio.Domain.Constants;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Services;

public class InventoryCalculator : IInventoryCalculator
{
    private class InventoryEntry
    {
        public decimal Quantity { get; set; }
        public decimal Cost { get; set; }
    }

    private class LossCandidate
    {
        public int TransactionId { get; set; }
        public string AssetId { get; set; } = default!;
        public DateTime Date { get; set; }
        public ProcessedTransaction ProcessedTransaction { get; set; } = default!;
    }

    public PortfolioReport CalculateInventory(IEnumerable<Transaction> transactions, FiatCurrency currency)
    {
        var sortedTransactions = transactions.OrderBy(t => t.Date).ToList();
        var processedTransactions = sortedTransactions.Select(t => new ProcessedTransaction(t)).ToList();
        
        var fifoQueue = new Dictionary<string, List<InventoryEntry>>();
        var lossCandidates = new List<LossCandidate>();

        foreach (var pTransaction in processedTransactions)
        {
            var transaction = pTransaction.Transaction;
            var type = transaction.TransactionTypeCode?.ToLowerInvariant() ?? ""; 
            var fromAssetId = transaction.FromAssetId;
            var toAssetId = transaction.ToAssetId;
            var feeAssetId = transaction.FeeAsset; 
            
            var amountReceived = transaction.AmountReceived;
            var amountSpent = transaction.AmountSpent;
            var fee = transaction.Fee;
            
            decimal costPerUnitOfTo = 0;
            if (amountReceived > 0)
            {
                 var fromAssetPrice = GetTransactionPrice(transaction, currency);
                 if (fromAssetPrice.HasValue)
                 {
                     var totalValue = amountSpent * fromAssetPrice.Value;
                     costPerUnitOfTo = totalValue / amountReceived;
                 }
            }

            var toAssetPrice = GetToAssetPrice(transaction, currency);
            
            // Skip cost calculation if price hasn't been calculated yet
            if (!toAssetPrice.HasValue)
            {
                continue; 
            }

            var costBasis = transaction.AmountReceived * toAssetPrice.Value;
            var feeAssetPrice = GetFeeAssetPrice(transaction, currency) ?? 0;

            // === 1. HANDLING INFLOWS (Buy/Swap-in/TransferIn) ===
            bool isInflow = false;
            string inflowAssetStr = string.Empty;
            decimal inflowQty = 0;
            decimal inflowCost = 0;

            if (type == "reward")
            {
                isInflow = true;
                inflowAssetStr = toAssetId;
                inflowQty = amountReceived;
                inflowCost = costPerUnitOfTo;

                // Reward genera ganancia directa
                var rewardProfit = amountReceived * toAssetPrice.Value;
                if (!string.IsNullOrEmpty(feeAssetId) && feeAssetPrice > 0)
                {
                    rewardProfit -= fee * feeAssetPrice;
                }
                
                if (pTransaction.ProfitLoss == null) 
                {
                    pTransaction.ProfitLoss = 0;
                }
                pTransaction.ProfitLoss += rewardProfit;
            }
            else if (type == "transferin")
            {
                isInflow = true;
                inflowAssetStr = toAssetId;
                inflowQty = amountReceived;
                inflowCost = costPerUnitOfTo;
            }
            else if (type == "swap" || type == "buy" || type == "sell")
            {
                if (amountReceived > 0 && !string.IsNullOrEmpty(toAssetId))
                {
                    isInflow = true;
                    inflowAssetStr = toAssetId;
                    inflowQty = amountReceived;
                    inflowCost = costPerUnitOfTo;
                }
                
                // Deduct Fee Expense
                if (!string.IsNullOrEmpty(feeAssetId) && feeAssetPrice > 0)
                {
                    if (pTransaction.ProfitLoss == null)
                    {
                        pTransaction.ProfitLoss = 0;
                    }
                    pTransaction.ProfitLoss -= fee * feeAssetPrice;
                }
            }

            if (isInflow && !string.IsNullOrEmpty(inflowAssetStr))
            {
                if (inflowAssetStr != CurrencyConstants.Eur && inflowAssetStr != CurrencyConstants.Usd)
                {
                    if (!fifoQueue.ContainsKey(inflowAssetStr)) 
                    {
                        fifoQueue[inflowAssetStr] = new List<InventoryEntry>();
                    }
                    fifoQueue[inflowAssetStr].Add(new InventoryEntry { Quantity = inflowQty, Cost = inflowCost });

                    CheckWashSale(lossCandidates, inflowAssetStr, transaction.Date, pTransaction);
                }
            }

            // === 2. HANDLING OUTFLOWS (Sell/Swap-out/Fee) ===
            
            if (!string.IsNullOrEmpty(feeAssetId) && fee > 0)
            {
                ConsumeInventory(fifoQueue, feeAssetId, fee, pTransaction, isFee: true, currency);
            }

            if ((type == "swap" || type == "buy" || type == "sell") && amountSpent > 0 && !string.IsNullOrEmpty(fromAssetId))
            {
                 ConsumeInventory(fifoQueue, fromAssetId, amountSpent, pTransaction, isFee: false, currency, lossCandidates: lossCandidates);
            }
        }

        var holdings = new List<AssetHolding>();
        foreach (var assetId in fifoQueue.Keys)
        {
            var entries = fifoQueue[assetId];
            var quantity = entries.Sum(e => e.Quantity);
            if (quantity > 0)
            {
                var totalCost = entries.Sum(e => e.Quantity * e.Cost);
                var avgCost = totalCost / quantity;
                
                // Calculate realized P/L for this asset
                var realizedPL = processedTransactions
                    .Where(t => (t.Transaction.FromAssetId == assetId || t.Transaction.ToAssetId == assetId) && t.ProfitLoss.HasValue && !t.IsLossDisallowed)
                    .Sum(t => t.ProfitLoss ?? 0m);

                holdings.Add(new AssetHolding
                {
                    AssetId = assetId,
                    Quantity = quantity,
                    AvgCost = avgCost,
                    RealizedProfitLoss = realizedPL
                });
            }
        }

        return new PortfolioReport
        {
            Transactions = processedTransactions,
            Holdings = holdings
        };
    }

    private void CheckWashSale(List<LossCandidate> lossCandidates, string assetId, DateTime purchaseDate, ProcessedTransaction currentTransaction)
    {
        int monthsLimit = 2;
        
        foreach (var candidate in lossCandidates)
        {
            if (candidate.AssetId == assetId && !candidate.ProcessedTransaction.IsLossDisallowed)
            {
                if (IsWithinXMonths(candidate.Date, purchaseDate, monthsLimit))
                {
                    candidate.ProcessedTransaction.IsLossDisallowed = true;
                    candidate.ProcessedTransaction.DisallowedByTransactionId = currentTransaction.Transaction.Id;
                    
                    currentTransaction.DisallowsPreviousLosses.Add(candidate.TransactionId);
                }
            }
        }
    }

    private bool IsWithinXMonths(DateTime from, DateTime to, int months)
    {
        var limitDate = from.AddMonths(months);
        return to > from && to <= limitDate;
    }

    private void ConsumeInventory(
        Dictionary<string, List<InventoryEntry>> queue,
        string assetId,
        decimal amountToConsume,
        ProcessedTransaction pTransaction,
        bool isFee,
        FiatCurrency currency,
        List<LossCandidate>? lossCandidates = null)
    {
        if (assetId == CurrencyConstants.Eur || assetId == CurrencyConstants.Usd) return;

        if (!queue.ContainsKey(assetId) || queue[assetId].Count == 0)
        {
            return;
        }

        decimal remaining = amountToConsume;
        decimal totalCostBasis = 0;

        while (remaining > 0 && queue[assetId].Count > 0)
        {
            var entry = queue[assetId][0];
            decimal quantityTaken = (entry.Quantity < remaining) ? entry.Quantity : remaining;

            totalCostBasis += quantityTaken * entry.Cost;
            entry.Quantity -= quantityTaken;
            remaining -= quantityTaken;

            if (entry.Quantity <= 0)
            {
                queue[assetId].RemoveAt(0);
            }
        }

        if (!isFee)
        {
            var transaction = pTransaction.Transaction;
            var fromPrice = GetTransactionPrice(transaction, currency);
            
            if (!fromPrice.HasValue) return; 
            
            decimal proceeds = transaction.AmountSpent * fromPrice.Value;

            decimal pl = proceeds - totalCostBasis;
            if (pTransaction.ProfitLoss == null) 
            {
                pTransaction.ProfitLoss = 0;
            }
            pTransaction.ProfitLoss += pl;

            if (pl < 0 && lossCandidates != null)
            {
                lossCandidates.Add(new LossCandidate
                {
                    TransactionId = transaction.Id,
                    AssetId = assetId,
                    Date = transaction.Date,
                    ProcessedTransaction = pTransaction
                });
            }

        }
    }

    private decimal? GetTransactionPrice(Transaction transaction, FiatCurrency currency)
    {
        return currency switch
        {
            FiatCurrency.USD => transaction.FromAssetPriceInUsd,
            FiatCurrency.EUR => transaction.FromAssetPriceInEur,
            _ => throw new ArgumentException($"Unsupported currency: {currency}")
        };
    }

    private decimal? GetToAssetPrice(Transaction transaction, FiatCurrency currency)
    {
        var fromPrice = GetTransactionPrice(transaction, currency);
        if (!fromPrice.HasValue)
        {
            return null;
        }
        
        if (transaction.ToAssetId == transaction.FromAssetId)
        {
            return fromPrice.Value;
        }
        
        if (transaction.AmountReceived == 0)
        {
            return null;
        }
        
        return (transaction.AmountSpent * fromPrice.Value) / transaction.AmountReceived;
    }

    private decimal? GetFeeAssetPrice(Transaction transaction, FiatCurrency currency)
    {
        return currency switch
        {
            FiatCurrency.USD => transaction.FeeAssetPriceInUsd,
            FiatCurrency.EUR => transaction.FeeAssetPriceInEur,
            _ => throw new ArgumentException($"Unsupported currency: {currency}")
        };
    }
}
