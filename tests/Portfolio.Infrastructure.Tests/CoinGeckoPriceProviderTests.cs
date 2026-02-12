using Microsoft.Extensions.Configuration;
using Portfolio.Domain.Enums;
using Portfolio.Infrastructure.ExternalServices;
using System.Net;
using Xunit;

namespace Portfolio.Infrastructure.Tests;

public class CoinGeckoPriceProviderTests
{
    // Simple fake Handler to avoid NSubstitute complexity with protected methods
    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, HttpResponseMessage> Check { get; set; } = _ => new HttpResponseMessage(HttpStatusCode.OK);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Check(request));
        }
    }

    [Fact]
    public async Task GetCurrentCryptoPricesAsync_ShouldParseApiResponseCorrectly()
    {
        FakeHttpMessageHandler fakeHandler = new()
        {
            Check = req =>
            {
                var json = "{\"bitcoin\":{\"usd\":50000},\"ethereum\":{\"usd\":3000}}";
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json)
                };
            }
        };

        HttpClient httpClient = new(fakeHandler);

        // Mock config
        Dictionary<string, string?> inMemorySettings = new() {
            {"CoinGecko:BaseUrl", "https://api.coingecko.com/api/v3/"},
            {"CoinGecko:ApiKey", "test-key"}
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        CoinGeckoPriceProvider provider = new(httpClient, configuration);
        
        var prices = await provider.GetCurrentCryptoPricesAsync(["bitcoin", "ethereum"], FiatCurrency.USD);
        
        Assert.Equal(50000m, prices["bitcoin"]);
        Assert.Equal(3000m, prices["ethereum"]);
    }
    
    [Fact]
    public async Task GetCurrentCryptoPricesAsync_ShouldReturnEmpty_WhenApiFails()
    {
        FakeHttpMessageHandler fakeHandler = new()
        {
            Check = req => new HttpResponseMessage(HttpStatusCode.BadRequest)
        };

        HttpClient httpClient = new(fakeHandler);
        var configuration = new ConfigurationBuilder().Build();
        CoinGeckoPriceProvider provider = new(httpClient, configuration);

        var prices = await provider.GetCurrentCryptoPricesAsync(["bitcoin"], FiatCurrency.USD);

        Assert.Empty(prices);
    }
}
