using System.ComponentModel.DataAnnotations;

namespace Portfolio.Server.Models.Requests
{
    public class NewTradeRequest
    {
        [Required]
        public string FromAssetId { get; set; } = null!;

        [Required]
        public string ToAssetId { get; set; } = null!;

        [Required]
        public decimal AmountSpent { get; set; }

        [Required]
        public decimal AmountReceived { get; set; }

        [Required]
        public decimal FromAssetPriceInEur { get; set; }

        [Required]
        public decimal Fee { get; set; }

        public string? FeeAsset { get; set; }

        public decimal? FeeAssetPriceInEur { get; set; }

        public string? Notes { get; set; }
    }
}
