using Portfolio.Domain.Enums;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Services;

/// <summary>
/// Domain service that derives and validates spot prices and fee prices for transactions.
/// <para>
/// <b>Pricing Policies:</b>
/// <list type="number">
///   <item>
///     <term>Implicit Fiat Parity</term>
///     <description>When the primary priced asset is itself a fiat currency (USD or EUR),
///     its spot price in that currency is always 1.0 (e.g. 1 EUR = 1.0 EUR).</description>
///   </item>
///   <item>
///     <term>Implied Disposal Price</term>
///     <description>When disposing of an asset for fiat (e.g. selling 2 AAPL for 300 USD),
///     the unit spot price of the asset sold is derived as (AmountReceived / AmountSpent).</description>
///   </item>
///   <item>
///     <term>Fee Fiat Parity</term>
///     <description>When transaction fees are paid in fiat (EUR or USD), the fee price
///     in that fiat is set to 1.0.</description>
///   </item>
///   <item>
///     <term>Validation</term>
///     <description>Ensures a reference currency exists and at least one spot price (USD or EUR)
///     is provided or derived.</description>
///   </item>
/// </list>
/// </para>
/// </summary>
public static class TransactionPriceResolver
{
    public static Price ResolveSpotPrice(
        TransactionType transactionType,
        Guid? fromAssetId,
        Guid? toAssetId,
        decimal amountSpent,
        decimal amountReceived,
        decimal? spotPriceUSD,
        decimal? spotPriceEUR,
        FiatCurrency? spotPriceInputCurrency)
    {
        // 1. Determine which asset is the primary "priced asset":
        bool isDisposalOrSwap = transactionType.RequiresFromAsset;
        Guid? pricedAssetId = isDisposalOrSwap ? fromAssetId : toAssetId;

        // 2. Case A: The priced asset is itself fiat currency.
        var pricedFiat = FiatCurrency.FromIdOrDefault(pricedAssetId);
        if (pricedFiat != null)
        {
            spotPriceInputCurrency = pricedFiat;
            if (pricedFiat == FiatCurrency.USD) spotPriceUSD = 1m;
            if (pricedFiat == FiatCurrency.EUR) spotPriceEUR = 1m;
        }
        // 3. Case B: Selling an asset for fiat (e.g. selling 2 AAPL for 300 USD).
        else if (isDisposalOrSwap && toAssetId.HasValue)
        {
            var counterpartyFiat = FiatCurrency.FromIdOrDefault(toAssetId);
            if (counterpartyFiat != null)
            {
                spotPriceInputCurrency = counterpartyFiat;
                decimal unitPriceInCounterpartyFiat = amountSpent > 0 ? amountReceived / amountSpent : 0m;

                if (counterpartyFiat == FiatCurrency.USD) spotPriceUSD = unitPriceInCounterpartyFiat;
                if (counterpartyFiat == FiatCurrency.EUR) spotPriceEUR = unitPriceInCounterpartyFiat;
            }
        }

        // 4. Validate that a reference currency and at least one price are established.
        ValidateRequiredPrices(spotPriceInputCurrency, spotPriceUSD, spotPriceEUR);

        return new Price(spotPriceUSD, spotPriceEUR, spotPriceInputCurrency!);
    }

    public static Price? ResolveFeePrice(
        Guid? feeAssetId,
        decimal? feeSpotPriceUSD,
        decimal? feeSpotPriceEUR,
        FiatCurrency? feePriceInputCurrency)
    {
        var feeFiat = FiatCurrency.FromIdOrDefault(feeAssetId);
        if (feeFiat != null)
        {
            feePriceInputCurrency = feeFiat;
            feeSpotPriceUSD = feeFiat == FiatCurrency.USD ? 1m : feeSpotPriceUSD;
            feeSpotPriceEUR = feeFiat == FiatCurrency.EUR ? 1m : feeSpotPriceEUR;
        }

        if (!feeAssetId.HasValue && !feeSpotPriceUSD.HasValue && !feeSpotPriceEUR.HasValue && feePriceInputCurrency == null)
        {
            return null;
        }

        return new Price(feeSpotPriceUSD, feeSpotPriceEUR, feePriceInputCurrency);
    }

    private static void ValidateRequiredPrices(
        FiatCurrency? spotPriceInputCurrency,
        decimal? spotPriceUSD,
        decimal? spotPriceEUR)
    {
        if (spotPriceInputCurrency == null)
            throw new ArgumentException("SpotPriceInputCurrency must be provided if the transaction does not implicitly involve a Fiat currency.");

        if (!spotPriceUSD.HasValue && !spotPriceEUR.HasValue)
            throw new ArgumentException("Either SpotPriceUSD or SpotPriceEUR must logically have a value depending on the Input Currency.");
    }
}
