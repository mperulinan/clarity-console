using System;
using System.Collections.Generic;

namespace Portfolio.Server.Models;

public partial class Trade
{
    public int Id { get; set; }

    public DateTime Date { get; set; }

    public string FromAssetCode { get; set; } = null!;

    public string ToAssetCode { get; set; } = null!;

    public decimal AmountSpent { get; set; }

    public decimal AmountReceived { get; set; }

    public decimal Fee { get; set; }

    public string? Notes { get; set; }
}
