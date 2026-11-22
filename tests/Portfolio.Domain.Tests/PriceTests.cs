using Portfolio.Domain.Enums;
using Portfolio.Domain.ValueObjects;
using Xunit;

namespace Portfolio.Domain.Tests;

public class PriceTests
{
    [Fact]
    public void IsIncomplete_WhenBothUsdAndEurPresent_ReturnsFalse()
    {
        var price = new Price(100m, 90m, FiatCurrency.USD);
        Assert.False(price.IsIncomplete);
    }

    [Fact]
    public void IsIncomplete_WhenUsdMissing_ReturnsTrue()
    {
        var price = new Price(null, 90m, FiatCurrency.EUR);
        Assert.True(price.IsIncomplete);
    }

    [Fact]
    public void IsIncomplete_WhenEurMissing_ReturnsTrue()
    {
        var price = new Price(100m, null, FiatCurrency.USD);
        Assert.True(price.IsIncomplete);
    }

    [Fact]
    public void WithDerivedRate_WhenInputCurrencyIsUsd_CalculatesEurPrice()
    {
        var price = new Price(100m, null, FiatCurrency.USD);
        var updated = price.WithDerivedRate(0.85m);

        Assert.Equal(100m, updated.USD);
        Assert.Equal(85m, updated.EUR);
        Assert.Equal(FiatCurrency.USD, updated.InputCurrency);
    }

    [Fact]
    public void WithDerivedRate_WhenInputCurrencyIsEur_CalculatesUsdPrice()
    {
        var price = new Price(null, 90m, FiatCurrency.EUR);
        var updated = price.WithDerivedRate(0.9m);

        Assert.Equal(100m, updated.USD);
        Assert.Equal(90m, updated.EUR);
        Assert.Equal(FiatCurrency.EUR, updated.InputCurrency);
    }

    [Fact]
    public void WithDerivedRate_WhenRateIsZeroOrNegative_ReturnsSameInstanceWithoutUpdate()
    {
        var price = new Price(100m, null, FiatCurrency.USD);
        var updatedZero = price.WithDerivedRate(0m);
        var updatedNeg = price.WithDerivedRate(-1m);

        Assert.Null(updatedZero.EUR);
        Assert.Null(updatedNeg.EUR);
    }

    [Fact]
    public void WithDerivedRate_WhenInputCurrencyIsNull_ReturnsSameInstanceWithoutUpdate()
    {
        var price = new Price(100m, null, null);
        var updated = price.WithDerivedRate(0.9m);

        Assert.Null(updated.EUR);
    }

    [Fact]
    public void GetPrice_ReturnsPriceForRequestedCurrency()
    {
        var price = new Price(120m, 100m, FiatCurrency.EUR);

        Assert.Equal(120m, price.GetPrice(FiatCurrency.USD));
        Assert.Equal(100m, price.GetPrice(FiatCurrency.EUR));
    }
}
