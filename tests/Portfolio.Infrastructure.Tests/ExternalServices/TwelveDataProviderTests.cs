using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Portfolio.Domain.Enums;
using Portfolio.Infrastructure.ExternalServices;
using Xunit;

namespace Portfolio.Infrastructure.Tests.ExternalServices;

public class TwelveDataProviderTests
{
    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "TwelveData:BaseUrl", "https://api.twelvedata.com/" },
                { "TwelveData:ApiKey", "test-key" }
            })
            .Build();

    [Theory]
    [InlineData("American Depositary Receipt")]
    [InlineData("Depositary Receipt")]
    [InlineData("Global Depositary Receipt")]
    [InlineData("ADR")]
    [InlineData("GDR")]
    [InlineData("Preferred Stock")]
    [InlineData("REIT")]
    public async Task SearchAssetsAsync_DepositaryReceiptAndEquityTypes_MapsToStock(string instrumentType)
    {
        // Arrange
        var responseJson = $$"""
            {
              "data": [
                {
                  "symbol": "BABA",
                  "instrument_name": "Alibaba Group Holding Ltd",
                  "instrument_type": "{{instrumentType}}",
                  "exchange": "NYSE"
                }
              ],
              "status": "ok"
            }
            """;

        var handler = new MockHttpMessageHandler(responseJson, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);
        var sut = new TwelveDataProvider(httpClient, BuildConfig(), NullLogger<TwelveDataProvider>.Instance);

        // Act
        var results = (await sut.SearchAssetsAsync("BABA")).ToList();

        // Assert
        Assert.Single(results);
        var match = results[0];
        Assert.Equal("BABA", match.Asset.Symbol);
        Assert.Equal("Alibaba Group Holding Ltd", match.Asset.Name);
        Assert.Equal(AssetType.Stock, match.Asset.Type);
        Assert.Equal("BABA", match.Asset.ExternalId);
    }

    [Theory]
    [InlineData("ETF")]
    [InlineData("Exchange-Traded Fund")]
    [InlineData("Closed-end Fund")]
    public async Task SearchAssetsAsync_EtfAndFundTypes_MapsToEtf(string instrumentType)
    {
        // Arrange
        var responseJson = $$"""
            {
              "data": [
                {
                  "symbol": "SPY",
                  "instrument_name": "SPDR S&P 500 ETF Trust",
                  "instrument_type": "{{instrumentType}}",
                  "exchange": "NYSE"
                }
              ],
              "status": "ok"
            }
            """;

        var handler = new MockHttpMessageHandler(responseJson, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);
        var sut = new TwelveDataProvider(httpClient, BuildConfig(), NullLogger<TwelveDataProvider>.Instance);

        // Act
        var results = (await sut.SearchAssetsAsync("SPY")).ToList();

        // Assert
        Assert.Single(results);
        Assert.Equal(AssetType.Etf, results[0].Asset.Type);
    }

    [Fact]
    public async Task SearchAssetsAsync_FallbackToTypeField_WhenInstrumentTypeIsNull()
    {
        // Arrange
        const string responseJson = """
            {
              "data": [
                {
                  "symbol": "BABA",
                  "instrument_name": "Alibaba Group Holding Ltd",
                  "type": "American Depositary Receipt",
                  "instrument_type": null,
                  "exchange": "NYSE"
                }
              ],
              "status": "ok"
            }
            """;

        var handler = new MockHttpMessageHandler(responseJson, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);
        var sut = new TwelveDataProvider(httpClient, BuildConfig(), NullLogger<TwelveDataProvider>.Instance);

        // Act
        var results = (await sut.SearchAssetsAsync("BABA")).ToList();

        // Assert
        Assert.Single(results);
        Assert.Equal(AssetType.Stock, results[0].Asset.Type);
    }

    [Fact]
    public async Task SearchAssetsAsync_FallbackToNameField_WhenInstrumentNameIsNull()
    {
        // Arrange
        const string responseJson = """
            {
              "data": [
                {
                  "symbol": "BABA",
                  "name": "Alibaba Group Holding Ltd",
                  "instrument_name": null,
                  "instrument_type": "American Depositary Receipt",
                  "exchange": "NYSE"
                }
              ],
              "status": "ok"
            }
            """;

        var handler = new MockHttpMessageHandler(responseJson, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);
        var sut = new TwelveDataProvider(httpClient, BuildConfig(), NullLogger<TwelveDataProvider>.Instance);

        // Act
        var results = (await sut.SearchAssetsAsync("BABA")).ToList();

        // Assert
        Assert.Single(results);
        Assert.Equal("Alibaba Group Holding Ltd", results[0].Asset.Name);
        Assert.Equal(AssetType.Stock, results[0].Asset.Type);
    }

    [Fact]
    public async Task SearchAssetsAsync_UnsupportedInstrumentType_IgnoresItem()
    {
        // Arrange
        const string responseJson = """
            {
              "data": [
                {
                  "symbol": "US10Y",
                  "instrument_name": "US 10-Year Bond",
                  "instrument_type": "Bond",
                  "exchange": "CBOE"
                }
              ],
              "status": "ok"
            }
            """;

        var handler = new MockHttpMessageHandler(responseJson, HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);
        var sut = new TwelveDataProvider(httpClient, BuildConfig(), NullLogger<TwelveDataProvider>.Instance);

        // Act
        var results = await sut.SearchAssetsAsync("US10Y");

        // Assert
        Assert.Empty(results);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task SearchAssetsAsync_EmptyOrWhitespaceQuery_ReturnsEmptyImmediately(string? query)
    {
        // Arrange
        var handler = new MockHttpMessageHandler("{}", HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);
        var sut = new TwelveDataProvider(httpClient, BuildConfig(), NullLogger<TwelveDataProvider>.Instance);

        // Act
        var results = await sut.SearchAssetsAsync(query!);

        // Assert
        Assert.Empty(results);
        Assert.Equal(0, handler.CallCount);
    }

    [Fact]
    public async Task SearchAssetsAsync_HttpError_ReturnsEmptySafelyWithoutThrowing()
    {
        // Arrange
        var handler = new MockHttpMessageHandler("Server error", HttpStatusCode.InternalServerError);
        var httpClient = new HttpClient(handler);
        var sut = new TwelveDataProvider(httpClient, BuildConfig(), NullLogger<TwelveDataProvider>.Instance);

        // Act
        var results = await sut.SearchAssetsAsync("BABA");

        // Assert
        Assert.Empty(results);
    }

    private class MockHttpMessageHandler(string response, HttpStatusCode statusCode) : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(response, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }
}

