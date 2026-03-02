using Portfolio.Domain.ValueObjects;

namespace Portfolio.Application.DTOs;

public class NewTransactionRequest
{
    public DateTime Date { get; set; }
    public TransactionType TransactionTypeCode { get; set; } = null!;
    public Guid FromAssetId { get; set; }
    public Guid ToAssetId { get; set; }
    public decimal AmountSpent { get; set; }
    public decimal AmountReceived { get; set; }
    public decimal FromAssetPriceInUsd { get; set; }
    public decimal? FromAssetPriceInEur { get; set; }
    public decimal Fee { get; set; }
    public Guid? FeeAssetId { get; set; }
    public decimal? FeeAssetPriceInUsd { get; set; }
    public decimal? FeeAssetPriceInEur { get; set; }
    public string? Notes { get; set; }
}
