using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Services;

public class InventoryCalculator : IInventoryCalculator
{
    const int monthsLimit = 2;

    private class InventoryEntry
    {
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
    }

    private class LossCandidate
    {
        public int TransactionId { get; set; }
        public Guid AssetId { get; set; } = default!;
        public DateTime Date { get; set; }
        public ProcessedTransaction ProcessedTransaction { get; set; } = default!;
    }

    public PortfolioReport CalculateInventory(IEnumerable<Transaction> transactions, FiatCurrency currency)
    {
        var processedTransactions = transactions
            .OrderBy(t => t.Date)
            .Select(t => new ProcessedTransaction(t))
            .ToList();

        Dictionary<Guid, Queue<InventoryEntry>> fifoQueue = [];
        Dictionary<Guid, List<LossCandidate>> lossCandidates = [];
        Dictionary<Guid, decimal> realizedPLTracker = [];
        Dictionary<Guid, decimal> costBasisSoldTracker = [];

        foreach (var pt in processedTransactions)
        {
            var tx = pt.Transaction;
            decimal toAssetPrice = tx.GetToAssetPrice(currency) ?? 0;
            decimal feeAssetPrice = tx.GetFeeAssetPrice(currency) ?? 0;

            // --- HANDLING INFLOWS (Swap-in/Deposit/Reward) ---
            if (tx.AmountReceived > 0 && tx.ToAssetId.HasValue)
            {
                Guid toAssetId = tx.ToAssetId.Value;
                if (tx.Type == TransactionType.Reward)
                {
                    decimal rewardProfit = (tx.AmountReceived * toAssetPrice) - (tx.Fee * feeAssetPrice);
                    pt.ProfitLoss = rewardProfit;

                    UpdateTracker(realizedPLTracker, toAssetId, rewardProfit);
                }

                if (!fifoQueue.TryGetValue(toAssetId, out var inventory))
                {
                    inventory = new Queue<InventoryEntry>();
                    fifoQueue[toAssetId] = inventory;
                }

                inventory.Enqueue(new InventoryEntry { Quantity = tx.AmountReceived, Price = toAssetPrice });

                if (tx.Type.TriggersWashSale)
                {
                    CheckWashSale(lossCandidates, toAssetId, tx.Date, pt);
                }
            }

            // --- HANDLING OUTFLOWS (Swap-out/Withdrawal/Fee) ---
            Guid feeAssetId = tx.FeeAssetId ?? Guid.Empty;
            if (tx.Fee > 0 && feeAssetId != Guid.Empty)
            {
                decimal feePL = ConsumeInventory(fifoQueue, feeAssetId, tx.Fee, pt, isFee: true, currency, costBasisSoldTracker);
                UpdateTracker(realizedPLTracker, feeAssetId, feePL);
            }

            if ((tx.Type == TransactionType.Swap || tx.Type == TransactionType.Withdrawal || tx.Type == TransactionType.Loss) && tx.AmountSpent > 0 && tx.FromAssetId.HasValue)
            {
                Guid fromAssetId = tx.FromAssetId.Value;
                decimal outflowPL = ConsumeInventory(fifoQueue, fromAssetId, tx.AmountSpent, pt, isFee: false, currency, costBasisSoldTracker, lossCandidates);
                UpdateTracker(realizedPLTracker, fromAssetId, outflowPL);
            }
        }

        List<AssetHolding> holdings = GenerateAssetHoldings(fifoQueue, realizedPLTracker, costBasisSoldTracker);

        return new PortfolioReport
        {
            Transactions = processedTransactions,
            Holdings = holdings
        };
    }

    private static void UpdateTracker(Dictionary<Guid, decimal> tracker, Guid assetId, decimal amount)
    {
        if (!tracker.ContainsKey(assetId))
        {
            tracker[assetId] = 0;
        }

        tracker[assetId] += amount;
    }

    private static List<AssetHolding> GenerateAssetHoldings(
        Dictionary<Guid, Queue<InventoryEntry>> fifoQueue,
        Dictionary<Guid, decimal> realizedPLTracker,
        Dictionary<Guid, decimal> costBasisSoldTracker)
    {
        List<AssetHolding> holdings = [];

        foreach (var assetId in fifoQueue.Keys)
        {
            var entries = fifoQueue[assetId];
            decimal totalQuantity = entries.Sum(e => e.Quantity);

            if (totalQuantity <= 0) continue;

            decimal totalRemainingCost = entries.Sum(e => e.Quantity * e.Price);
            decimal avgCost = totalRemainingCost / totalQuantity;

            realizedPLTracker.TryGetValue(assetId, out decimal realizedPL);
            costBasisSoldTracker.TryGetValue(assetId, out decimal costBasisSold);

            holdings.Add(new AssetHolding
            {
                Id = assetId,
                Quantity = totalQuantity,
                AvgCost = avgCost,
                CostBasisOfSold = costBasisSold,
                RealizedPL = realizedPL
            });
        }

        return holdings;
    }

    private static void CheckWashSale(
        Dictionary<Guid, List<LossCandidate>> lossCandidates,
        Guid assetId,
        DateTime purchaseDate,
        ProcessedTransaction currentTransaction)
    {
        if (!lossCandidates.TryGetValue(assetId, out var candidates)) return;

        foreach (var candidate in candidates)
        {
            if (candidate.ProcessedTransaction.IsLossDisallowed) continue;

            if (IsWithinXMonths(candidate.Date, purchaseDate, monthsLimit))
            {
                candidate.ProcessedTransaction.IsLossDisallowed = true;
                candidate.ProcessedTransaction.DisallowedByTransactionId = currentTransaction.Transaction.Id;
                currentTransaction.DisallowsPreviousLosses.Add(candidate.TransactionId);
            }
        }
    }

    private static bool IsWithinXMonths(DateTime from, DateTime to, int months)
    {
        DateTime limitDate = from.AddMonths(months);
        return to > from && to <= limitDate;
    }

    private static decimal ConsumeInventory(
        Dictionary<Guid, Queue<InventoryEntry>> queue,
        Guid assetId,
        decimal amountToConsume,
        ProcessedTransaction pTransaction,
        bool isFee,
        FiatCurrency currency,
        Dictionary<Guid, decimal> costBasisSoldTracker,
        Dictionary<Guid, List<LossCandidate>>? lossCandidates = null)
    {
        if (!queue.TryGetValue(assetId, out var inventory) || inventory.Count == 0)
        {
            pTransaction.Error = $"Insufficient inventory for {GetAssetSymbol(pTransaction.Transaction, assetId)}. Needed {amountToConsume}.";
            return 0;
        }

        decimal totalCostBasis = DequeueInventory(inventory, amountToConsume, out decimal remaining);

        if (remaining > 0)
        {
             pTransaction.Error = $"Insufficient inventory for {GetAssetSymbol(pTransaction.Transaction, assetId)}. Missing {remaining}, used {amountToConsume - remaining}.";
        }

        var tx = pTransaction.Transaction;
        decimal? exitPrice = isFee ? tx.GetFeeAssetPrice(currency) : tx.GetFromAssetPrice(currency);
        if (!exitPrice.HasValue) return 0;

        decimal proceeds = (!isFee && tx.Type == TransactionType.Loss) ? 0 : amountToConsume * exitPrice.Value;
        
        UpdateTracker(costBasisSoldTracker, assetId, totalCostBasis);

        decimal pl = proceeds - totalCostBasis;
        pTransaction.ProfitLoss = (pTransaction.ProfitLoss ?? 0) + pl;
        
        if (!isFee && pl < 0 && lossCandidates != null)
        {
            RecordLossCandidate(lossCandidates, assetId, tx, pTransaction);
        }

        return pl;
    }

    private static decimal DequeueInventory(Queue<InventoryEntry> inventory, decimal amountToConsume, out decimal remaining)
    {
        remaining = amountToConsume;
        decimal totalCostBasis = 0;

        while (remaining > 0 && inventory.Count > 0)
        {
            var entry = inventory.Peek();
            decimal quantityTaken = Math.Min(entry.Quantity, remaining);

            totalCostBasis += quantityTaken * entry.Price;
            entry.Quantity -= quantityTaken;
            remaining -= quantityTaken;

            if (entry.Quantity <= 0)
            {
                inventory.Dequeue();
            }
        }

        return totalCostBasis;
    }

    private static string GetAssetSymbol(Transaction tx, Guid assetId)
    {
        if (tx.FromAssetId == assetId && tx.FromAsset != null) return tx.FromAsset.Symbol;
        if (tx.ToAssetId == assetId && tx.ToAsset != null) return tx.ToAsset.Symbol;
        if (tx.FeeAssetId == assetId && tx.FeeAsset != null) return tx.FeeAsset.Symbol;
        return assetId.ToString();
    }

    private static void RecordLossCandidate(
        Dictionary<Guid, List<LossCandidate>> lossCandidates,
        Guid assetId,
        Transaction tx,
        ProcessedTransaction pTransaction)
    {
        if (!lossCandidates.TryGetValue(assetId, out var assetLossCandidates))
        {
            assetLossCandidates = [];
            lossCandidates[assetId] = assetLossCandidates;
        }

        assetLossCandidates.Add(new LossCandidate
        {
            TransactionId = tx.Id,
            AssetId = assetId,
            Date = tx.Date,
            ProcessedTransaction = pTransaction
        });
    }
}
