using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.Application.DTOs;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Infrastructure.Persistence;
using Xunit;

namespace Portfolio.API.Tests.Controllers;

[Collection("API collection")]
public class PortfolioControllerTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly CustomWebApplicationFactory<Program> _factory = factory;

    [Fact]
    public async Task GetPortfolioMetrics_ReturnsMetricsSuccessfully()
    {
        // Arrange - Seed some data
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PortfolioContext>();


            var usd = new Asset("USD", "US Dollar", null, null, AssetType.Fiat);
            var btc = new Asset("BTC", "Bitcoin", null, null, AssetType.Crypto);


            context.Assets.AddRange(usd, btc);
            context.SaveChanges();

            var tx = new Transaction(DateTime.UtcNow, TransactionType.Swap, usd.Id, btc.Id, 1000, 1, 1000, null, 0, null, null, null, null, FiatCurrency.USD, null, null);
            context.Transactions.Add(tx);
            context.SaveChanges();
        }

        // Act
        var response = await _client.GetAsync("/api/Portfolio/metrics");

        // Assert
        response.EnsureSuccessStatusCode();
        var metrics = await response.Content.ReadFromJsonAsync<PortfolioMetricsDto>();


        Assert.NotNull(metrics);
        // The metrics should not be empty since we seeded a transaction
        Assert.True(metrics.TotalPortfolioValueUsd > 0 || metrics.TotalCostBasisUsd > 0);
    }
}
