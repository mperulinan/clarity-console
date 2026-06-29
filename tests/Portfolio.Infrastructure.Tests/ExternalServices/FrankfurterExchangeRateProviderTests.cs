using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Portfolio.Domain.Enums;
using Portfolio.Infrastructure.ExternalServices;

namespace Portfolio.Infrastructure.Tests.ExternalServices;

public class FrankfurterExchangeRateProviderTests
{
    private readonly TimeProvider _timeProviderMock;
    private readonly IConfiguration _configuration;

    public FrankfurterExchangeRateProviderTests()
    {
        _timeProviderMock = Substitute.For<TimeProvider>();
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Frankfurter:BaseUrl", "https://api.frankfurter.app/" }
            })
            .Build();
    }

    [Fact]
    public async Task GetUsdEurRateAsync_TodayOrFutureDate_ThrowsNotSupportedException()
    {
        // Arrange
        var today = DateTimeOffset.UtcNow;
        _timeProviderMock.GetUtcNow().Returns(today);

        var httpClient = new HttpClient();
        var provider = new FrankfurterExchangeRateProvider(httpClient, _configuration, NullLogger<FrankfurterExchangeRateProvider>.Instance, _timeProviderMock);

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() => provider.GetUsdEurRateAsync(today.DateTime));
        await Assert.ThrowsAsync<NotSupportedException>(() => provider.GetUsdEurRateAsync(today.DateTime.AddDays(1)));
    }

    [Fact]
    public async Task GetEurUsdRateAsync_TodayOrFutureDate_ThrowsNotSupportedException()
    {
        // Arrange
        var today = DateTimeOffset.UtcNow;
        _timeProviderMock.GetUtcNow().Returns(today);

        var httpClient = new HttpClient();
        var provider = new FrankfurterExchangeRateProvider(httpClient, _configuration, NullLogger<FrankfurterExchangeRateProvider>.Instance, _timeProviderMock);

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() => provider.GetEurUsdRateAsync(today.DateTime));
        await Assert.ThrowsAsync<NotSupportedException>(() => provider.GetEurUsdRateAsync(today.DateTime.AddDays(1)));
    }

    [Fact]
    public async Task GetExchangeRateAsync_ValidResponse_ReturnsRate()
    {
        // Arrange
        var expectedRate = 1.05m;
        var responseContent = new
        {
            amount = 1.0m,
            @base = "USD",
            date = "2026-06-28",
            rates = new Dictionary<string, decimal> { { "EUR", expectedRate } }
        };

        var handler = new MockHttpMessageHandler(JsonSerializer.Serialize(responseContent), HttpStatusCode.OK);
        var httpClient = new HttpClient(handler);
        var provider = new FrankfurterExchangeRateProvider(httpClient, _configuration, NullLogger<FrankfurterExchangeRateProvider>.Instance, _timeProviderMock);

        // Act
        var result = await provider.GetExchangeRateAsync(FiatCurrency.USD, FiatCurrency.EUR);

        // Assert
        Assert.Equal(expectedRate, result);
    }
    
    [Fact]
    public async Task GetExchangeRateAsync_SameCurrency_ReturnsOne()
    {
        // Arrange
        var httpClient = new HttpClient();
        var provider = new FrankfurterExchangeRateProvider(httpClient, _configuration, NullLogger<FrankfurterExchangeRateProvider>.Instance, _timeProviderMock);

        // Act
        var result = await provider.GetExchangeRateAsync(FiatCurrency.USD, FiatCurrency.USD);

        // Assert
        Assert.Equal(1.0m, result);
    }

    private class MockHttpMessageHandler(string response, HttpStatusCode statusCode) : HttpMessageHandler
    {
        private readonly string _response = response;
        private readonly HttpStatusCode _statusCode = statusCode;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage
            {
                StatusCode = _statusCode,
                Content = new StringContent(_response, System.Text.Encoding.UTF8, "application/json")
            });
        }
    }
}
