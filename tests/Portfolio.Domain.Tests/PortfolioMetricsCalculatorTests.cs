using Portfolio.Domain.Services;
using Portfolio.Domain.ValueObjects;
using Xunit;

namespace Portfolio.Domain.Tests;

public class PortfolioMetricsCalculatorTests
{
    private readonly PortfolioMetricsCalculator _sut = new();

    private static AssetHolding MakeHolding(
        decimal quantity, decimal avgCost,
        decimal realizedPL = 0, decimal costBasisOfSold = 0) =>
        new()
        {
            Id = Guid.NewGuid(),
            Quantity = quantity,
            AvgCost = avgCost,
            RealizedPL = realizedPL,
            CostBasisOfSold = costBasisOfSold
        };

    [Fact]
    public void CalculateMetrics_WhenNoHoldings_ReturnsAllZerosAndEmptyList()
    {
        var result = _sut.CalculateMetrics([], []);

        Assert.Equal(0, result.TotalPortfolioValueUsd);
        Assert.Equal(0, result.TotalCostBasisUsd);
        Assert.Equal(0, result.NetDepositsUsd);
        Assert.Equal(0, result.TotalUnrealizedProfitLossUsd);
        Assert.Equal(0, result.TotalRealizedProfitLossUsd);
        Assert.Equal(0, result.TotalProfitLossUsd);
        Assert.Equal(0, result.UnrealizedProfitLossPercentage);
        Assert.Equal(0, result.TotalReturn);
        Assert.Empty(result.Holdings);
    }

    [Fact]
    public void CalculateMetrics_SingleHolding_ComputesCurrentValueAndOpenPL()
    {
        var holding = MakeHolding(quantity: 2m, avgCost: 1000m);
        var prices = new Dictionary<Guid, decimal> { { holding.Id, 1500m } };

        var result = _sut.CalculateMetrics([holding], prices);
        var h = result.Holdings.Single();

        Assert.Equal(3000m, h.CurrentValue);    // 2 * 1500
        Assert.Equal(2000m, h.TotalCostBasis);  // 2 * 1000
        Assert.Equal(1000m, h.OpenPL);          // 3000 - 2000
        Assert.Equal(50m, h.OpenReturn);        // (1500/1000 - 1) * 100
    }

    [Fact]
    public void CalculateMetrics_MultipleHoldings_AggregatesPortfolioTotals()
    {
        var holding1 = MakeHolding(quantity: 1m, avgCost: 1000m);
        var holding2 = MakeHolding(quantity: 5m, avgCost: 100m);
        var holdings = new List<AssetHolding> { holding1, holding2 };
        var prices = new Dictionary<Guid, decimal>
        {
            { holding1.Id, 2000m }, // H1: value=2000, cost=1000, openPL=+1000
            { holding2.Id, 80m   }  // H2: value=400,  cost=500,  openPL=-100
        };

        var result = _sut.CalculateMetrics(holdings, prices);

        Assert.Equal(2400m, result.TotalPortfolioValueUsd);
        Assert.Equal(1500m, result.TotalCostBasisUsd);
        Assert.Equal(900m, result.TotalUnrealizedProfitLossUsd);
    }

    [Fact]
    public void CalculateMetrics_AllocationPercentages_SumTo100()
    {
        var holding1 = MakeHolding(quantity: 1m, avgCost: 1m);
        var holding2 = MakeHolding(quantity: 3m, avgCost: 1m);
        var holdings = new List<AssetHolding> { holding1, holding2 };
        var prices = new Dictionary<Guid, decimal>
        {
            { holding1.Id, 1m },
            { holding2.Id, 1m }
        };

        var result = _sut.CalculateMetrics(holdings, prices);
        var sum = result.Holdings.Sum(h => h.AllocationPercentage);

        Assert.Equal(100m, sum);
        Assert.Equal(25m, result.Holdings.First().AllocationPercentage);
        Assert.Equal(75m, result.Holdings.Last().AllocationPercentage);
    }

    [Fact]
    public void CalculateMetrics_WithRealizedPL_IncludesInTotalPL()
    {
        // Sold half the position at a profit of 500
        var holding = MakeHolding(quantity: 1m, avgCost: 1000m, realizedPL: 500m, costBasisOfSold: 1000m);

        var prices = new Dictionary<Guid, decimal> { { holding.Id, 1000m } };
        var result = _sut.CalculateMetrics([holding], prices);

        Assert.Equal(500m, result.TotalRealizedProfitLossUsd);
        Assert.Equal(500m, result.TotalProfitLossUsd); // openPL=0, realized=500
    }

    [Fact]
    public void CalculateMetrics_MissingPrice_DefaultsToZero()
    {
        var holding = MakeHolding(quantity: 5m, avgCost: 200m);

        var result = _sut.CalculateMetrics([holding], []);
        var h = result.Holdings.Single();

        Assert.Equal(0m, h.CurrentPrice);
        Assert.Equal(0m, h.CurrentValue);
        Assert.Equal(-1000m, h.OpenPL); // 0 - (5*200)
    }
}
