using Microsoft.Extensions.Configuration;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.ValueObjects;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Portfolio.Infrastructure.ExternalServices;

public class CoinGeckoProvider(HttpClient httpClient, IConfiguration configuration)
    : ICryptoMarketDataProvider
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly string _baseUrl = configuration["CoinGecko:BaseUrl"] ?? "https://api.coingecko.com/api/v3/";
    private readonly string? _apiKey = configuration["CoinGecko:ApiKey"];

    public record CoinGeckoMarketDto(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("symbol")] string Symbol,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("current_price")] decimal? Price,
        [property: JsonPropertyName("image")] string? ImageUrl
    );

    public async Task<Dictionary<string, AssetMarketData>> GetCryptoMarketDataAsync(IEnumerable<string> assetIds, FiatCurrency priceCurrency)
    {
        var cryptoIds = assetIds.Distinct().ToList();
        if (cryptoIds.Count == 0)
        {
            return new Dictionary<string, AssetMarketData>(StringComparer.OrdinalIgnoreCase);
        }

        string idsParam = string.Join(",", cryptoIds.Select(id => id.ToLower()));
        string url = $"{_baseUrl}coins/markets?vs_currency={priceCurrency.Value.ToLower()}&ids={idsParam}&x_cg_demo_api_key={_apiKey}";

        try
        {
            var dtos = await _httpClient.GetFromJsonAsync<List<CoinGeckoMarketDto>>(url);
            if (dtos == null) {
                return [];
            }

            return dtos.ToDictionary(
                d => d.Id,
                d => new AssetMarketData(
                    d.Symbol,
                    d.Name,
                    d.Price ?? 0,
                    d.ImageUrl),
                StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            return new Dictionary<string, AssetMarketData>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
