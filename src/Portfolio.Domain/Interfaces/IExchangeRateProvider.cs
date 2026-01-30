using Portfolio.Domain.Enums;

namespace Portfolio.Domain.Interfaces;

public interface IExchangeRateProvider
{
    Task<decimal> GetUsdEurRateAsync(DateTime date);
    Task<decimal> GetEurUsdRateAsync(DateTime date);
    
    /// <summary>
    /// Gets the latest exchange rate from one currency to another.
    /// </summary>
    Task<decimal> GetExchangeRateAsync(FiatCurrency from, FiatCurrency to);
}
