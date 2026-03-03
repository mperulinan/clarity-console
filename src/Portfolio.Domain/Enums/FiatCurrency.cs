using Ardalis.SmartEnum;

namespace Portfolio.Domain.Enums;

public class FiatCurrency : SmartEnum<FiatCurrency, string>
{
    public static readonly FiatCurrency USD = new("USD", "usd", Guid.Parse("0200F65C-EA84-4ED6-B1C0-EB36527F11ED"));
    public static readonly FiatCurrency EUR = new("EUR", "eur", Guid.Parse("2574E866-B50C-41EA-9293-CE8964AEFCD2"));

    public Guid Id { get; }

    private FiatCurrency(string name, string value, Guid id) : base(name, value)
    {
        Id = id;
    }
    
    public static FiatCurrency Parse(string symbol)
    {
        if (TryFromValue(symbol.ToLowerInvariant(), out var currency))
        {
            return currency;
        }

        throw new ArgumentException($"Unknown fiat currency: {symbol}");
    }
}
