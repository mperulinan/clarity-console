using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Portfolio.Application.DTOs;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Infrastructure.Persistence;
using Xunit;

namespace Portfolio.API.Tests.Controllers;

[Collection("API collection")]
public class TransactionControllerTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly CustomWebApplicationFactory<Program> _factory = factory;

    /// <summary>
    /// Seeds a USD and BTC asset in the database and returns their IDs.
    /// Each test calls this independently to avoid cross-test data dependency.
    /// </summary>
    private (Guid usdId, Guid btcId) SeedAssets()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PortfolioContext>();

        var usd = new Asset(FiatCurrency.USD.Symbol, FiatCurrency.USD.Name, null, null, AssetType.Fiat);
        var btc = new Asset("BTC", "Bitcoin", null, null, AssetType.Crypto);

        context.Assets.AddRange(usd, btc);
        context.SaveChanges();

        return (usd.Id, btc.Id);
    }

    [Fact]
    public async Task GetTransactions_ReturnsTransactionList()
    {
        // Arrange
        var (usdId, btcId) = SeedAssets();
        var jsonOptions = _factory.GetJsonOptions();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PortfolioContext>();
        var tx = new Transaction(DateTime.UtcNow, TransactionType.Swap, usdId, btcId, 1000, 0.1m, 10000, null, 0, null, null, null, null, FiatCurrency.USD, null, null);
        context.Transactions.Add(tx);
        context.SaveChanges();

        // Act
        var response = await _client.GetAsync("/api/Transaction");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<IEnumerable<TransactionDto>>(jsonOptions);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains(result, t => t.Id == tx.Id);
    }

    [Fact]
    public async Task GetTransaction_ExistingId_ReturnsTransaction()
    {
        // Arrange
        var (usdId, btcId) = SeedAssets();
        var jsonOptions = _factory.GetJsonOptions();

        int txId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PortfolioContext>();
            var tx = new Transaction(DateTime.UtcNow, TransactionType.Swap, usdId, btcId, 500, 0.05m, 10000, null, 0, null, null, null, null, FiatCurrency.USD, null, null);
            context.Transactions.Add(tx);
            context.SaveChanges();
            txId = tx.Id;
        }

        // Act
        var response = await _client.GetAsync($"/api/Transaction/{txId}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<TransactionDto>(jsonOptions);
        Assert.NotNull(result);
        Assert.Equal(txId, result.Id);
    }

    [Fact]
    public async Task GetTransaction_NonExistingId_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/Transaction/999999999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostTransaction_ValidData_ReturnsCreatedTransaction()
    {
        // Arrange — seed assets locally, no dependency on other tests
        var (usdId, btcId) = SeedAssets();
        var jsonOptions = _factory.GetJsonOptions();

        var request = new NewTransactionRequest
        {
            Date = DateTime.UtcNow,
            TransactionTypeCode = TransactionType.Swap,
            FromAssetId = usdId,
            ToAssetId = btcId,
            AmountSpent = 1500,
            AmountReceived = 0.03m,
            SpotPriceUSD = 50000,
            SpotPriceInputCurrency = "USD"
        };

        // Act — must use the API's JsonSerializerOptions so TransactionType is serialized
        // as its string value (SmartEnum), not as a plain object, which would cause a 400.
        var response = await _client.PostAsJsonAsync("/api/Transaction", request, jsonOptions);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<TransactionDto>(jsonOptions);

        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal(request.AmountSpent, result.AmountSpent);
        Assert.Equal(request.AmountReceived, result.AmountReceived);
        Assert.Equal(request.FromAssetId, result.FromAsset?.Id);
        Assert.Equal(request.ToAssetId, result.ToAsset?.Id);
    }
}
