namespace Portfolio.Domain.Interfaces;

public interface IExchangeRateProvider
{
    Task<decimal> GetUsdEurRateAsync(DateTime date);
    Task<decimal> GetEurUsdRateAsync(DateTime date);
}
