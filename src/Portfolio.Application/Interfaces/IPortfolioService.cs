using Portfolio.Application.DTOs;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Application.Interfaces;

public interface IPortfolioService
{
    Task<PortfolioReport> GetPortfolioReportAsync();
    Task<PortfolioMetrics> GetPortfolioMetricsAsync();
    Task AddTransactionAsync(NewTransactionRequest request);
    Task CalculateExchangeRatesAsync(); // New method for lazy calculation
}
