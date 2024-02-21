using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Portfolio.Server.Models;

public partial class Trade
{
    public int Id { get; set; }

    public DateTime Date { get; set; }

    [Required]
    public string FromAssetCode { get; set; } = null!;

    [Required]
    public string ToAssetCode { get; set; } = null!;

    [Required]
    public decimal AmountSpent { get; set; }

    [Required]
    public decimal AmountReceived { get; set; }

    [Required]
    public decimal Fee { get; set; }

    public string? Notes { get; set; }
}
