using Ardalis.SmartEnum;

namespace Portfolio.Domain.Enums;

public class FiatCurrency : SmartEnum<FiatCurrency, string>
{
    public static readonly FiatCurrency USD = new("USD", "usd");
    public static readonly FiatCurrency EUR = new("EUR", "eur");

    private FiatCurrency(string name, string value) : base(name, value)
    {
    }
    
    public static bool IsFiat(string code) => 
        TryFromValue(code.ToLowerInvariant(), out _);

    public static FiatCurrency Parse(string code)
    {
        if (TryFromValue(code.ToLowerInvariant(), out var currency))
        {
            return currency;
        }

        throw new ArgumentException($"Unknown fiat currency: {code}");
    }
}
