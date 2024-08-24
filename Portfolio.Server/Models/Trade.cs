using System;
using System.Collections.Generic;

namespace Portfolio.Server.Models;

public partial class Trade
{
    public int Id { get; set; }

    public DateTime Date { get; set; }

    public string TransactionType { get; set; } = null!;

    public string FromAssetId { get; set; } = null!;

    public string ToAssetId { get; set; } = null!;

    public decimal AmountSpent { get; set; }

    public decimal AmountReceived { get; set; }

    public decimal FromAssetPriceInEur { get; set; }

    public decimal Fee { get; set; }

    public string? FeeAsset { get; set; }

    public decimal? FeeAssetPriceInEur { get; set; }

    public string? Notes { get; set; }

    public virtual TransactionType TransactionTypeNavigation { get; set; } = null!;
}
