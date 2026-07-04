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
public class AssetControllerTests(CustomWebApplicationFactory<Program> factory) : IClassFixture<CustomWebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly CustomWebApplicationFactory<Program> _factory = factory;

    [Fact]
    public async Task GetAssets_ReturnsAssetList()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PortfolioContext>();
        
        var asset = new Asset(
            symbol: "ETH",
            name: "Ethereum",
            externalId: "ethereum",
            imageUrl: "https://example.com/eth.png",
            type: AssetType.Crypto
        );
        context.Assets.Add(asset);
        context.SaveChanges();

        // Act
        var response = await _client.GetAsync("/api/Asset");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<IEnumerable<AssetDto>>(_factory.GetJsonOptions());
        
        Assert.NotNull(result);
        Assert.Contains(result, a => a.Symbol == "ETH");
    }

    [Fact]
    public async Task GetFiatCurrencies_ReturnsStaticList()
    {
        // Act
        var response = await _client.GetAsync("/api/Asset/fiat-currencies");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<IEnumerable<AssetDto>>(_factory.GetJsonOptions());
        
        Assert.NotNull(result);
        Assert.Contains(result, a => a.Symbol == "USD");
        Assert.Contains(result, a => a.Symbol == "EUR");
    }

    [Fact]
    public async Task GetTaxCurrency_ReturnsConfiguredTaxCurrency()
    {
        // Act
        var response = await _client.GetAsync("/api/Asset/tax-currency");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AssetDto>(_factory.GetJsonOptions());
        
        Assert.NotNull(result);
        Assert.Equal(FiatCurrency.TaxCurrency.Value.ToUpperInvariant(), result.Symbol);
    }

    [Fact]
    public async Task GetDefaultCurrency_ReturnsConfiguredDefaultCurrency()
    {
        // Act
        var response = await _client.GetAsync("/api/Asset/default-currency");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AssetDto>(_factory.GetJsonOptions());
        
        Assert.NotNull(result);
        Assert.Equal(FiatCurrency.DefaultDisplayCurrency.Value.ToUpperInvariant(), result.Symbol);
    }

    [Fact]
    public async Task GetSpotPrice_ExistingAsset_DoesNotReturn5xx()
    {
        // Arrange
        Guid ethId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PortfolioContext>();
            var asset = new Asset(
                symbol: "ETH",
                name: "Ethereum",
                externalId: "ethereum",
                imageUrl: "https://example.com/eth.png",
                type: AssetType.Crypto
            );
            context.Assets.Add(asset);
            context.SaveChanges();
            ethId = asset.Id;
        }

        // Act — use the real seeded asset ID.
        // The external price provider (CoinGecko) may or may not be available in CI without API keys,
        // so we accept either 200 OK (price found) or 404 (price returned null).
        // What we must never get is a 5xx server error.
        var response = await _client.GetAsync($"/api/Asset/{ethId}/price?fiatCurrency=usd");

        // Assert
        Assert.True(
            (int)response.StatusCode < 500,
            $"Expected no server error, but got {(int)response.StatusCode} {response.StatusCode}");
    }

    [Fact]
    public async Task GetSpotPrice_NonExistingAsset_ReturnsNotFound()
    {
        // Act — Guid.NewGuid() will never exist in the database
        var response = await _client.GetAsync($"/api/Asset/{Guid.NewGuid()}/price?fiatCurrency=usd");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
