using Portfolio.Domain.Enums;

namespace Portfolio.Application.DTOs;

public class NewTransactionRequest
{
    public DateTime Date { get; set; }
    public TransactionTypeEnum TransactionTypeCode { get; set; } = null!;
    public string FromAssetId { get; set; } = null!;
    public string ToAssetId { get; set; } = null!;
    public decimal AmountSpent { get; set; }
    public decimal AmountReceived { get; set; }
    public decimal FromAssetPriceInUsd { get; set; }
    public decimal? FromAssetPriceInEur { get; set; }
    public decimal Fee { get; set; }
    public string? FeeAsset { get; set; }
    public decimal? FeeAssetPriceInUsd { get; set; }
    public decimal? FeeAssetPriceInEur { get; set; }
    public string? Notes { get; set; }
}
