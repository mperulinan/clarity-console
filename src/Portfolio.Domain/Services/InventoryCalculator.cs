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
            string type = transaction.TransactionTypeCode;
            string fromAssetId = transaction.FromAssetId;
            string toAssetId = transaction.ToAssetId;
            string? feeAssetId = transaction.FeeAsset;

            decimal amountReceived = transaction.AmountReceived;
            decimal amountSpent = transaction.AmountSpent;
            decimal fee = transaction.Fee;

            decimal? toAssetPrice = transaction.GetToAssetValue(currency);

            if (!toAssetPrice.HasValue)
            {
                continue;
            }

            decimal feeAssetPrice = transaction.GetFeeAssetValue(currency) ?? 0;

            // === 1. HANDLING INFLOWS (Swap-in/TransferIn/Reward) ===
            bool isInflow = false;
            string inflowAssetStr = string.Empty;
            decimal inflowQty = 0;
            decimal inflowCost = 0;

            // 1. Calculate General Inflow (Common for Swap, TransferIn, Reward)
            if (amountReceived > 0 && !string.IsNullOrWhiteSpace(toAssetId))
            {
                isInflow = true;
                inflowAssetStr = toAssetId;
                inflowQty = amountReceived;
                inflowCost = transaction.GetToAssetValue(currency) ?? 0;
            }

            switch (transaction.Type.Name)
            {
                case nameof(TransactionTypeEnum.Reward):
                    if (isInflow)
                    {
                        // Override Cost Basis: Market Price (Income)
                        inflowCost = toAssetPrice.Value;

                        // Reward generates direct profit
                        var rewardProfit = amountReceived * toAssetPrice.Value;
                        if (!string.IsNullOrWhiteSpace(feeAssetId) && feeAssetPrice > 0)
                        {
                            rewardProfit -= fee * feeAssetPrice;
                        }

                        pTransaction.ProfitLoss ??= 0;
                        pTransaction.ProfitLoss += rewardProfit;
                    }
                    break;

                case nameof(TransactionTypeEnum.Swap):
                    // Deduct Fee Expense
                    if (!string.IsNullOrWhiteSpace(feeAssetId) && feeAssetPrice > 0)
                    {
                        pTransaction.ProfitLoss ??= 0;
                        pTransaction.ProfitLoss -= fee * feeAssetPrice;
                    }
                    break;
            }

            if (isInflow && !string.IsNullOrWhiteSpace(inflowAssetStr))
            {
                if (!fifoQueue.TryGetValue(inflowAssetStr, out List<InventoryEntry>? inventoryEntries))
                {
                    inventoryEntries = [];
                    fifoQueue[inflowAssetStr] = inventoryEntries;
                }

                inventoryEntries.Add(new InventoryEntry { Quantity = inflowQty, Cost = inflowCost });

                CheckWashSale(lossCandidates, inflowAssetStr, transaction.Date, pTransaction);
            }

            // === 2. HANDLING OUTFLOWS (Swap-out/Fee) ===

            if (!string.IsNullOrWhiteSpace(feeAssetId) && fee > 0)
            {
                ConsumeInventory(fifoQueue, feeAssetId, fee, pTransaction, isFee: true, currency);
            }

            if (type == TransactionTypeEnum.Swap && amountSpent > 0 && !string.IsNullOrWhiteSpace(fromAssetId))
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
        if (FiatCurrency.IsFiat(assetId)) return;

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
            var fromPrice = transaction.GetFromAssetValue(currency);

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
}
