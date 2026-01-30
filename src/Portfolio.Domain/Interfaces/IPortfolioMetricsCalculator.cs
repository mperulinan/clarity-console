using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Interfaces;

public interface IPortfolioMetricsCalculator
{
    PortfolioMetrics CalculateMetrics(
        List<AssetHolding> holdings, 
        Dictionary<string, decimal> currentPricesUsd);
}
