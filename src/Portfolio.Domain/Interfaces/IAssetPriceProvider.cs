using Portfolio.Domain.Enums;

namespace Portfolio.Domain.Interfaces;

public interface IAssetPriceProvider
{
    Task<Dictionary<string, decimal>> GetPricesAsync(IEnumerable<string> externalIds, FiatCurrency currency);
    Task<decimal?> GetHistoricalPriceAsync(string externalId, FiatCurrency currency, DateTime date);
}
