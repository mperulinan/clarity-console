using Portfolio.Domain.ValueObjects;

namespace Portfolio.Application.DTOs;

public class TransactionDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public TransactionType Type { get; set; } = null!;
    public AssetDto? FromAsset { get; set; }
    public AssetDto? ToAsset { get; set; }
    public decimal AmountSpent { get; set; }
    public decimal AmountReceived { get; set; }
    public decimal? SpotPriceUSD { get; set; }
    public decimal? SpotPriceEUR { get; set; }
    public decimal Fee { get; set; }
    public AssetDto? FeeAsset { get; set; }
    public decimal? FeePriceUSD { get; set; }
    public decimal? FeePriceEUR { get; set; }
    public decimal? UsdEurExchangeRate { get; set; }
    public string? Notes { get; set; }
}

public class ProcessedTransactionDto
{
    public TransactionDto Transaction { get; set; } = null!;
    public decimal? ProfitLoss { get; set; }
    public decimal? TotalLossAmount { get; set; }
    public bool IsLossDisallowed { get; set; }
    public int? DisallowedByTransactionId { get; set; }
    public List<int> DisallowsPreviousLosses { get; set; } = [];
    public string? Error { get; set; }
}
