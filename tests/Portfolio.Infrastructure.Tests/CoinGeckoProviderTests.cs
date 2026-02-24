using Microsoft.Extensions.Configuration;
using Portfolio.Domain.Enums;
using Portfolio.Infrastructure.ExternalServices;
using System.Net;
using System.Text;
using Xunit;

namespace Portfolio.Infrastructure.Tests;

public class CoinGeckoProviderTests
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

    private static IConfiguration BuildConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "CoinGecko:BaseUrl", "https://api.coingecko.com/api/v3/" },
                { "CoinGecko:ApiKey", "test-key" }
            })
            .Build();

    // ── GetCurrentCryptoPricesAsync ──────────────────────────────────────────

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
        CoinGeckoProvider provider = new(httpClient, BuildConfig());

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
        CoinGeckoProvider provider = new(httpClient, BuildConfig());

        var prices = await provider.GetCurrentCryptoPricesAsync(["bitcoin"], FiatCurrency.USD);

        Assert.Empty(prices);
    }

    [Fact]
    public async Task GetCurrentCryptoPricesAsync_ShouldIgnoreFiatCurrencies()
    {
        bool apiWasCalled = false;
        FakeHttpMessageHandler fakeHandler = new()
        {
            Check = req => { apiWasCalled = true; return new HttpResponseMessage(HttpStatusCode.OK); }
        };

        HttpClient httpClient = new(fakeHandler);
        CoinGeckoProvider provider = new(httpClient, BuildConfig());

        var prices = await provider.GetCurrentCryptoPricesAsync(["usd", "eur"], FiatCurrency.USD);

        Assert.False(apiWasCalled, "API should not be called for fiat-only asset lists.");
        Assert.Empty(prices);
    }

    // ── GetAssetImageUrlsAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetAssetImageUrlsAsync_ShouldReturnImageUrls_WhenApiSucceeds()
    {
        const string marketsJson = """
            [
              { "id": "bitcoin",  "image": "https://coin-images.coingecko.com/coins/images/1/large/bitcoin.png" },
              { "id": "ethereum", "image": "https://coin-images.coingecko.com/coins/images/279/large/ethereum.png" }
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

        var images = await provider.GetAssetImageUrlsAsync(["bitcoin", "ethereum"]);

        Assert.Equal("https://coin-images.coingecko.com/coins/images/1/large/bitcoin.png", images["bitcoin"]);
        Assert.Equal("https://coin-images.coingecko.com/coins/images/279/large/ethereum.png", images["ethereum"]);
    }

    [Fact]
    public async Task GetAssetImageUrlsAsync_ShouldReturnEmpty_WhenApiFails()
    {
        FakeHttpMessageHandler fakeHandler = new()
        {
            Check = req => new HttpResponseMessage(HttpStatusCode.TooManyRequests) // 429
        };

        HttpClient httpClient = new(fakeHandler);
        CoinGeckoProvider provider = new(httpClient, BuildConfig());

        var images = await provider.GetAssetImageUrlsAsync(["bitcoin"]);

        Assert.Empty(images);
    }

    [Fact]
    public async Task GetAssetImageUrlsAsync_ShouldIgnoreFiatCurrencies()
    {
        bool apiWasCalled = false;
        FakeHttpMessageHandler fakeHandler = new()
        {
            Check = req => { apiWasCalled = true; return new HttpResponseMessage(HttpStatusCode.OK); }
        };

        HttpClient httpClient = new(fakeHandler);
        CoinGeckoProvider provider = new(httpClient, BuildConfig());

        var images = await provider.GetAssetImageUrlsAsync(["usd", "eur"]);

        Assert.False(apiWasCalled, "API should not be called for fiat-only asset lists.");
        Assert.Empty(images);
    }
}
