using Microsoft.Extensions.Configuration;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.ValueObjects;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Portfolio.Infrastructure.ExternalServices;

public class CoinGeckoProvider(HttpClient httpClient, IConfiguration configuration)
    : IAssetPriceProvider, IAssetCatalogProvider, IAssetSearchProvider
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly string _baseUrl = configuration["CoinGecko:BaseUrl"] ?? "https://api.coingecko.com/api/v3/";
    private readonly string? _apiKey = configuration["CoinGecko:ApiKey"];

    public async Task<Dictionary<string, decimal>> GetPricesAsync(IEnumerable<string> externalIds, FiatCurrency currency)
    {
        var ids = externalIds.Distinct().ToList();
        if (ids.Count == 0) return [];

        string idsParam = string.Join(",", ids.Select(id => id.ToLower()));
        string vsCurrency = currency.Value.ToLower();
        string url = $"{_baseUrl}simple/price?ids={idsParam}&vs_currencies={vsCurrency}&x_cg_demo_api_key={_apiKey}";

        try
        {
            var response = await _httpClient.GetFromJsonAsync<Dictionary<string, Dictionary<string, decimal>>>(url);
            if (response == null) return [];

            var prices = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in response)
            {
                if (kvp.Value.TryGetValue(vsCurrency, out var price))
                {
                    prices[kvp.Key] = price;
                }
            }
            return prices;
        }
        catch (Exception)
        {
            return [];
        }
    }

    public record CoinGeckoMarketDto(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("symbol")] string Symbol,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("image")] string? ImageUrl
    );

    public async Task<IEnumerable<Asset>> GetTopAssetsAsync(AssetType type, int count = 250)
    {
        if (type != AssetType.Crypto) return [];

        string url = $"{_baseUrl}coins/markets?vs_currency={FiatCurrency.USD.Value}&per_page={count}&page=1&x_cg_demo_api_key={_apiKey}";
        return await FetchMarketAssetsAsync(url);
    }

    public async Task<IEnumerable<Asset>> GetAssetsByExternalIdsAsync(AssetType type, IEnumerable<string> externalIds)
    {
        if (type != AssetType.Crypto) return [];

        var idsList = externalIds.Distinct().ToList();
        if (idsList.Count == 0) return [];

        string idsParam = string.Join(",", idsList.Select(id => id.ToLower()));
        string url = $"{_baseUrl}coins/markets?vs_currency={FiatCurrency.USD.Value}&ids={idsParam}&x_cg_demo_api_key={_apiKey}";
        return await FetchMarketAssetsAsync(url);
    }

    private async Task<IEnumerable<Asset>> FetchMarketAssetsAsync(string url)
    {
        try
        {
            var dtos = await _httpClient.GetFromJsonAsync<List<CoinGeckoMarketDto>>(url);
            if (dtos == null) return [];

            return dtos.Select(d => new Asset(
                d.Symbol.ToUpper(),
                d.Name,
                d.Id,
                d.ImageUrl,
                AssetType.Crypto
            ));
        }
        catch (Exception)
        {
            return [];
        }
    }

    public record CoinGeckoSearchItemDto(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("symbol")] string Symbol,
        [property: JsonPropertyName("large")] string? ImageUrl
    );

    public record CoinGeckoSearchResponseDto(
        [property: JsonPropertyName("coins")] List<CoinGeckoSearchItemDto> Coins
    );

    public async Task<IEnumerable<Asset>> SearchAssetsAsync(AssetType type, string query)
    {
        if (type != AssetType.Crypto || string.IsNullOrWhiteSpace(query)) return [];

        string url = $"{_baseUrl}search?query={Uri.EscapeDataString(query)}&x_cg_demo_api_key={_apiKey}";

        try
        {
            var response = await _httpClient.GetFromJsonAsync<CoinGeckoSearchResponseDto>(url);
            if (response?.Coins == null) return [];

            return response.Coins.Select(c => new Asset(
                c.Symbol.ToUpper(),
                c.Name,
                c.Id,
                c.ImageUrl,
                AssetType.Crypto
            ));
        }
        catch (Exception)
        {
            return [];
        }
    }
}
