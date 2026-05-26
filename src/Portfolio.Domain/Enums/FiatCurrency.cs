using Ardalis.SmartEnum;

namespace Portfolio.Domain.Enums;

public class FiatCurrency : SmartEnum<FiatCurrency, string>
{
    public static readonly FiatCurrency USD = new("US Dollar", "usd", Guid.Parse("0200F65C-EA84-4ED6-B1C0-EB36527F11ED"), "USD", "https://static.okx.com/cdn/oksupport/asset/currency/icon/usd.png");
    public static readonly FiatCurrency EUR = new("Euro", "eur", Guid.Parse("2574E866-B50C-41EA-9293-CE8964AEFCD2"), "EUR", "https://static.okx.com/cdn/oksupport/asset/currency/icon/eur.png");
    /// <summary>
    /// The currency used for tax reporting. Defined as a property (not a field)
    /// so SmartEnum does not include it in its auto-discovered List.
    /// </summary>
    public static FiatCurrency TaxCurrency => EUR;

    public Guid Id { get; }
    public string Symbol { get; }
    public string? ImageUrl { get; }

    private FiatCurrency(string name, string value, Guid id, string symbol, string? imageUrl = null) : base(name, value)
    {
        Id = id;
        Symbol = symbol;
        ImageUrl = imageUrl;
    }
    
    public static FiatCurrency Parse(string symbol)
    {
        if (TryFromValue(symbol.ToLowerInvariant(), out var currency))
        {
            return currency;
        }

        throw new ArgumentException($"Unknown fiat currency: {symbol}");
    }

    public static FiatCurrency? FromIdOrDefault(Guid? id)
    {
        return id.HasValue ? List.FirstOrDefault(f => f.Id == id.Value) : null;
    }
}
