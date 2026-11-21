using Portfolio.Domain.Enums;

namespace Portfolio.Domain.ValueObjects;

/// <summary>
/// Result of resolving the spot price and reference currency for a transaction's primary asset.
/// <para>
/// <b>Terminology:</b>
/// <list type="bullet">
///   <item>
///     <term>Priced Asset</term>
///     <description>The primary asset whose market price is tracked by SpotPrice.
///     For disposals/swaps (<c>RequiresFromAsset == true</c>), it is <c>FromAsset</c>.
///     For acquisitions/deposits/rewards (<c>RequiresFromAsset == false</c>), it is <c>ToAsset</c>.
///     </description>
///   </item>
///   <item>
///     <term>InputCurrency (Reference Currency)</term>
///     <description>The fiat currency (USD or EUR) serving as the original source of truth
///     for this transaction. Cross-currency exchange rates are computed from this base.
///     </description>
///   </item>
/// </list>
/// </para>
/// </summary>
public readonly record struct DerivedSpotPrice(
    decimal? PriceUSD,
    decimal? PriceEUR,
    FiatCurrency InputCurrency
);

/// <summary>
/// Result of resolving the fee price and reference currency for a transaction's fee asset.
/// </summary>
public readonly record struct DerivedFeePrice(
    decimal? PriceUSD,
    decimal? PriceEUR,
    FiatCurrency? InputCurrency
);
