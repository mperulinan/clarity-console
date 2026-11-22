using Portfolio.Domain.Enums;

namespace Portfolio.Domain.ValueObjects;

/// <summary>
/// Value Object encapsulating an asset's unit price across supported fiat currencies (USD and EUR),
/// tracking the original reference currency (source of truth) and providing conversion capabilities.
/// </summary>
public record Price
{
    public decimal? USD { get; init; }
    public decimal? EUR { get; init; }
    public FiatCurrency? InputCurrency { get; init; }

    // Parameterless constructor for EF Core serialization/materialization
    private Price() { }

    public Price(decimal? usd, decimal? eur, FiatCurrency? inputCurrency)
    {
        USD = usd;
        EUR = eur;
        InputCurrency = inputCurrency;
    }

    /// <summary>
    /// Indicates whether either USD or EUR price is missing and requires exchange rate synchronization.
    /// </summary>
    public bool IsIncomplete => !USD.HasValue || !EUR.HasValue;

    /// <summary>
    /// Returns a new <see cref="Price"/> with the missing currency price derived
    /// from the reference currency using the provided USD-to-EUR exchange rate.
    /// </summary>
    public Price WithDerivedRate(decimal usdEurRate)
    {
        if (InputCurrency == null || usdEurRate <= 0)
            return this;

        decimal? newUsd = USD;
        decimal? newEur = EUR;

        if (InputCurrency == FiatCurrency.USD && USD.HasValue)
        {
            newEur = USD.Value * usdEurRate;
        }
        else if (InputCurrency == FiatCurrency.EUR && EUR.HasValue)
        {
            newUsd ??= EUR.Value / usdEurRate;
        }

        return this with { USD = newUsd, EUR = newEur };
    }

    /// <summary>
    /// Gets the price for a specific fiat currency, throwing if unsupported.
    /// </summary>
    public decimal? GetPrice(FiatCurrency currency)
    {
        if (currency == FiatCurrency.USD) return USD;
        if (currency == FiatCurrency.EUR) return EUR;
        throw new ArgumentException($"Unsupported currency: {currency.Name}");
    }
}
