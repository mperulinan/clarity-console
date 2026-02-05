using Ardalis.SmartEnum;

namespace Portfolio.Domain.Enums;

public class FiatCurrency : SmartEnum<FiatCurrency, string>
{
    public static readonly FiatCurrency USD = new("USD", "usd");
    public static readonly FiatCurrency EUR = new("EUR", "eur");

    private FiatCurrency(string name, string value) : base(name, value)
    {
    }
}
