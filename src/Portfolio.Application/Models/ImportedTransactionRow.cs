using Portfolio.Domain.Enums;

namespace Portfolio.Application.Models;

public record ImportedTransactionRow(
    DateTime Date,
    string ProductName,
    string Isin,
    string Exchange,
    decimal Quantity,           // positive = buy, negative = sell
    decimal PricePerUnit,
    string PriceCurrency,       // e.g. "USD", "EUR"
    decimal? FxRate,
    decimal? FeeEur,
    decimal TotalEur,
    string BrokerTransactionId,
    TransactionType? TransactionType = null
);
