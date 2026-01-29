using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Services;

public interface IPortfolioMetricsCalculator
{
    PortfolioMetrics CalculateMetrics(
        List<AssetHolding> holdings, 
        Dictionary<string, decimal> currentPricesUsd);
}
