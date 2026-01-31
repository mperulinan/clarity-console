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

        Dictionary<string, List<InventoryEntry>> fifoQueue = [];
        List<LossCandidate> lossCandidates = [];

        foreach (var pTransaction in processedTransactions)
        {
            var transaction = pTransaction.Transaction;
            string type = transaction.TransactionTypeCode?.ToLowerInvariant() ?? "";
            string fromAssetId = transaction.FromAssetId;
            string toAssetId = transaction.ToAssetId;
            string? feeAssetId = transaction.FeeAsset;
            
            decimal amountReceived = transaction.AmountReceived;
            decimal amountSpent = transaction.AmountSpent;
            decimal fee = transaction.Fee;
            
            decimal costPerUnitOfTo = 0;
            if (amountReceived > 0)
            {
                decimal? fromAssetPrice = GetFromAssetPrice(transaction, currency);
                if (fromAssetPrice.HasValue)
                {
                    decimal totalValue = amountSpent * fromAssetPrice.Value;
                    costPerUnitOfTo = totalValue / amountReceived;
                }
            }

            decimal? toAssetPrice = GetToAssetPrice(transaction, currency);
            
            // Skip cost calculation if price hasn't been calculated yet
            if (!toAssetPrice.HasValue)
            {
                continue;
            }

            decimal costBasis = transaction.AmountReceived * toAssetPrice.Value;
            decimal feeAssetPrice = GetFeeAssetPrice(transaction, currency) ?? 0;

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
                if (!string.IsNullOrWhiteSpace(feeAssetId) && feeAssetPrice > 0)
                {
                    rewardProfit -= fee * feeAssetPrice;
                }
                
                pTransaction.ProfitLoss ??= 0;
                pTransaction.ProfitLoss += rewardProfit;
            }
            else if (type == "transfer_in")
            {
                isInflow = true;
                inflowAssetStr = toAssetId;
                inflowQty = amountReceived;
                inflowCost = costPerUnitOfTo;
            }
            else if (type == "swap")
            {
                if (amountReceived > 0 && !string.IsNullOrWhiteSpace(toAssetId))
                {
                    isInflow = true;
                    inflowAssetStr = toAssetId;
                    inflowQty = amountReceived;
                    inflowCost = costPerUnitOfTo;
                }
                
                // Deduct Fee Expense
                if (!string.IsNullOrWhiteSpace(feeAssetId) && feeAssetPrice > 0)
                {
                    pTransaction.ProfitLoss ??= 0;
                    pTransaction.ProfitLoss -= fee * feeAssetPrice;
                }
            }

            if (isInflow && !string.IsNullOrWhiteSpace(inflowAssetStr))
            {
                if (!fifoQueue.TryGetValue(inflowAssetStr, out List<InventoryEntry>? value))
                {
                    value = [];
                    fifoQueue[inflowAssetStr] = value;
                }

                value.Add(new InventoryEntry { Quantity = inflowQty, Cost = inflowCost });

                CheckWashSale(lossCandidates, inflowAssetStr, transaction.Date, pTransaction);
            }

            // === 2. HANDLING OUTFLOWS (Sell/Swap-out/Fee) ===

            if (!string.IsNullOrWhiteSpace(feeAssetId) && fee > 0)
            {
                ConsumeInventory(fifoQueue, feeAssetId, fee, pTransaction, isFee: true, currency);
            }

            if (type == "swap" && amountSpent > 0 && !string.IsNullOrWhiteSpace(fromAssetId))
            {
                ConsumeInventory(fifoQueue, fromAssetId, amountSpent, pTransaction, isFee: false, currency, lossCandidates: lossCandidates);
            }
        }

        List<AssetHolding> holdings = [];
        foreach (var assetId in fifoQueue.Keys)
        {
            var entries = fifoQueue[assetId];
            decimal quantity = entries.Sum(e => e.Quantity);
            if (quantity > 0)
            {
                decimal totalCost = entries.Sum(e => e.Quantity * e.Cost);
                decimal avgCost = totalCost / quantity;
                
                // Calculate realized P/L for this asset
                decimal realizedPL = processedTransactions
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

    private static void CheckWashSale(List<LossCandidate> lossCandidates, string assetId, DateTime purchaseDate, ProcessedTransaction currentTransaction)
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

    private static bool IsWithinXMonths(DateTime from, DateTime to, int months)
    {
        DateTime limitDate = from.AddMonths(months);
        return to > from && to <= limitDate;
    }

    private static void ConsumeInventory(
        Dictionary<string, List<InventoryEntry>> queue,
        string assetId,
        decimal amountToConsume,
        ProcessedTransaction pTransaction,
        bool isFee,
        FiatCurrency currency,
        List<LossCandidate>? lossCandidates = null)
    {
        if (!queue.TryGetValue(assetId, out List<InventoryEntry>? value) || value.Count == 0)
        {
            return;
        }

        decimal remaining = amountToConsume;
        decimal totalCostBasis = 0;

        while (remaining > 0 && value.Count > 0)
        {
            var entry = value[0];
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
            var fromPrice = GetFromAssetPrice(transaction, currency);
            
            if (!fromPrice.HasValue) return; 
            
            decimal proceeds = transaction.AmountSpent * fromPrice.Value;

            decimal pl = proceeds - totalCostBasis;
            pTransaction.ProfitLoss ??= 0;
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

    private static decimal? GetFromAssetPrice(Transaction transaction, FiatCurrency currency)
    {
        return currency switch
        {
            FiatCurrency.USD => transaction.FromAssetPriceInUsd,
            FiatCurrency.EUR => transaction.FromAssetPriceInEur,
            _ => throw new ArgumentException($"Unsupported currency: {currency}")
        };
    }

    private static decimal? GetToAssetPrice(Transaction transaction, FiatCurrency currency)
    {
        decimal? fromPrice = GetFromAssetPrice(transaction, currency);
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
        
        return transaction.AmountSpent * fromPrice.Value / transaction.AmountReceived;
    }

    private static decimal? GetFeeAssetPrice(Transaction transaction, FiatCurrency currency)
    {
        return currency switch
        {
            FiatCurrency.USD => transaction.FeeAssetPriceInUsd,
            FiatCurrency.EUR => transaction.FeeAssetPriceInEur,
            _ => throw new ArgumentException($"Unsupported currency: {currency}")
        };
    }
}
