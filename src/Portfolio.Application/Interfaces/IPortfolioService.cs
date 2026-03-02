using Portfolio.Application.DTOs;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Application.Interfaces;

public interface IPortfolioService
{
    Task<PortfolioReportDto> GetPortfolioReportAsync();
    Task<PortfolioMetrics> GetPortfolioMetricsAsync();
    Task AddTransactionAsync(NewTransactionRequest request);
    Task CalculateExchangeRatesAsync(); // New method for lazy calculation
}
