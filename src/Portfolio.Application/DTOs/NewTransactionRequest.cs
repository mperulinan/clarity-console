using Portfolio.Domain.ValueObjects;

namespace Portfolio.Application.DTOs;

public class NewTransactionRequest
{
    public DateTime Date { get; set; }
    public TransactionType TransactionTypeCode { get; set; } = null!;
    public Guid? FromAssetId { get; set; }
    public Guid? ToAssetId { get; set; }
    public decimal AmountSpent { get; set; }
    public decimal AmountReceived { get; set; }
    public decimal? SpotPriceUSD { get; set; }
    public decimal? SpotPriceEUR { get; set; }
    public string? SpotPriceInputCurrency { get; set; }
    public decimal Fee { get; set; }
    public Guid? FeeAssetId { get; set; }
    public decimal? FeePriceUSD { get; set; }
    public decimal? FeePriceEUR { get; set; }
    public string? FeePriceInputCurrency { get; set; }
    public string? Notes { get; set; }
}
