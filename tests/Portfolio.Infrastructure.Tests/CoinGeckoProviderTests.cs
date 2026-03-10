using Microsoft.Extensions.Configuration;
using Portfolio.Domain.Enums;
using Portfolio.Infrastructure.ExternalServices;
using System.Net;
using System.Text;
using Xunit;

namespace Portfolio.Infrastructure.Tests;

public class CoinGeckoProviderTests
{
    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, HttpResponseMessage> Check { get; set; } = _ => new HttpResponseMessage(HttpStatusCode.OK);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Check(request));
        }
    }

    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "CoinGecko:BaseUrl", "https://api.coingecko.com/api/v3/" },
                { "CoinGecko:ApiKey", "test-key" }
            })
            .Build();

    [Fact]
    public async Task GetPricesAsync_ShouldParsePriceCorrectly()
    {
        const string pricesJson = """
            {
              "bitcoin": { "usd": 70000 },
              "ethereum": { "usd": 3000 }
            }
            """;

        FakeHttpMessageHandler fakeHandler = new()
        {
            Check = req => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(pricesJson, Encoding.UTF8, "application/json")
            }
        };

        HttpClient httpClient = new(fakeHandler);
        CoinGeckoProvider provider = new(httpClient, BuildConfig());

        var data = await provider.GetPricesAsync(["bitcoin", "ethereum"], FiatCurrency.USD);

        Assert.Equal(70000m, data["bitcoin"]);
        Assert.Equal(3000m, data["ethereum"]);
    }

    [Fact]
    public async Task GetPricesAsync_ShouldReturnEmpty_WhenApiFails()
    {
        FakeHttpMessageHandler fakeHandler = new()
        {
            Check = req => new HttpResponseMessage(HttpStatusCode.TooManyRequests) // 429
        };

        HttpClient httpClient = new(fakeHandler);
        CoinGeckoProvider provider = new(httpClient, BuildConfig());

        var data = await provider.GetPricesAsync(["bitcoin"], FiatCurrency.USD);

        Assert.Empty(data);
    }
}
