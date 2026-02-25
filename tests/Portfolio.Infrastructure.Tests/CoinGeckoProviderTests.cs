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
    public async Task GetCryptoMarketDataAsync_ShouldParsePriceAndImageCorrectly()
    {
        const string marketsJson = """
            [
              { "id": "bitcoin",  "current_price": 70000, "image": "https://bitcoin.png" },
              { "id": "ethereum", "current_price": 3000, "image": "https://ethereum.png" }
            ]
            """;

        FakeHttpMessageHandler fakeHandler = new()
        {
            Check = req => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(marketsJson, Encoding.UTF8, "application/json")
            }
        };

        HttpClient httpClient = new(fakeHandler);
        CoinGeckoProvider provider = new(httpClient, BuildConfig());

        // Domain layer guarantees we only pass cryptos here
        var data = await provider.GetCryptoMarketDataAsync(["bitcoin", "ethereum"], FiatCurrency.USD);

        Assert.Equal(70000m, data["bitcoin"].Price);
        Assert.Equal("https://bitcoin.png", data["bitcoin"].ImageUrl);
        
        Assert.Equal(3000m, data["ethereum"].Price);
        Assert.Equal("https://ethereum.png", data["ethereum"].ImageUrl);
    }

    [Fact]
    public async Task GetCryptoMarketDataAsync_ShouldReturnEmpty_WhenApiFails()
    {
        FakeHttpMessageHandler fakeHandler = new()
        {
            Check = req => new HttpResponseMessage(HttpStatusCode.TooManyRequests) // 429
        };

        HttpClient httpClient = new(fakeHandler);
        CoinGeckoProvider provider = new(httpClient, BuildConfig());

        var data = await provider.GetCryptoMarketDataAsync(["bitcoin"], FiatCurrency.USD);

        Assert.Empty(data);
    }
}
