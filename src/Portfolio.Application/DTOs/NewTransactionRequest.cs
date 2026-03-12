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
    public decimal SpotPriceInUsd { get; set; }
    public decimal? SpotPriceInEur { get; set; }
    public decimal Fee { get; set; }
    public Guid? FeeAssetId { get; set; }
    public decimal? FeeSpotPriceInUsd { get; set; }
    public decimal? FeeSpotPriceInEur { get; set; }
    public string? Notes { get; set; }
}
