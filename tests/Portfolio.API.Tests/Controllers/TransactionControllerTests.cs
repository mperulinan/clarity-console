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
    public async Task GetProcessedTransactions_ReturnsProcessedTransactionList()
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
        var response = await _client.GetAsync("/api/Transaction/processed?currency=USD");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<IEnumerable<ProcessedTransactionDto>>(jsonOptions);

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.Contains(result, pt => pt.Transaction.Id == tx.Id);
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

        // Act 1: Create the transaction
        var postResponse = await _client.PostAsJsonAsync("/api/Transaction", request, jsonOptions);
        postResponse.EnsureSuccessStatusCode();
        var createdResult = await postResponse.Content.ReadFromJsonAsync<TransactionDto>(jsonOptions);

        Assert.NotNull(createdResult);
        Assert.True(createdResult.Id > 0);
        Assert.Equal(request.AmountSpent, createdResult.AmountSpent);
        Assert.Equal(request.AmountReceived, createdResult.AmountReceived);

        // Act 2: Fetch the created transaction to verify the database saved the asset links
        var getResponse = await _client.GetAsync($"/api/Transaction/{createdResult.Id}");
        getResponse.EnsureSuccessStatusCode();
        var fetchedResult = await getResponse.Content.ReadFromJsonAsync<TransactionDto>(jsonOptions);

        // Assert
        Assert.NotNull(fetchedResult);
        Assert.Equal(createdResult.Id, fetchedResult.Id);
        Assert.NotNull(fetchedResult.FromAsset);
        Assert.NotNull(fetchedResult.ToAsset);
        Assert.Equal(request.FromAssetId, fetchedResult.FromAsset.Id);
        Assert.Equal(request.ToAssetId, fetchedResult.ToAsset.Id);
    }
}
