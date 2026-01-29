using Portfolio.Application.DTOs;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Application.Interfaces;

public interface IPortfolioService
{
    Task<PortfolioReport> GetPortfolioAsync();
    Task<PortfolioMetrics> GetPortfolioDashboardAsync();
    Task AddTransactionAsync(NewTransactionRequest request);
    Task CalculateExchangeRatesAsync(); // New method for lazy calculation
}
