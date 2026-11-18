using Portfolio.Domain.Enums;

namespace Portfolio.Application.Models;

/// <summary>
/// Intermediate representation of a single imported broker record prior to classification and domain mapping.
/// <para>
/// Operates in one of two distinct modes:
/// <list type="bullet">
///   <item>
///     <term>Trade Execution Mode (<see cref="TransactionType"/> is <c>null</c>)</term>
///     <description>
///       Represents security buy/sell orders (Swap transactions).
///       <see cref="Quantity"/> represents signed units traded (positive = buy, negative = sell),
///       and fields such as <see cref="Exchange"/>, <see cref="PricePerUnit"/>, <see cref="FxRate"/>,
///       and <see cref="FeeEur"/> reflect execution terms.
///     </description>
///   </item>
///   <item>
///     <term>Account Cash Mode (<see cref="TransactionType"/> is non-null)</term>
///     <description>
///       Represents cash transactions such as deposits, withdrawals, dividends (<see cref="TransactionType.Reward"/>),
///       or connectivity/ADR fees (<see cref="TransactionType.Loss"/>).
///       <see cref="Quantity"/> reflects the absolute cash amount, <see cref="PricePerUnit"/> defaults to 1m,
///       <see cref="TotalAmount"/> carries the signed cash variation in <see cref="TotalCurrency"/>, and execution-specific fields
///       (<see cref="Exchange"/>, <see cref="FxRate"/>, <see cref="FeeEur"/>) are unused or defaulted.
///     </description>
///   </item>
/// </list>
/// </para>
/// </summary>
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
    decimal TotalAmount,
    string TotalCurrency,
    string BrokerTransactionId,
    TransactionType? TransactionType = null
);
