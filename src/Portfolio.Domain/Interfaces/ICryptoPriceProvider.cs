using Portfolio.Domain.Enums;

namespace Portfolio.Domain.Interfaces;

public interface ICryptoPriceProvider
{
    Task<Dictionary<string, decimal>> GetCurrentCryptoPricesAsync(IEnumerable<string> assetIds, FiatCurrency priceCurrency);
}
