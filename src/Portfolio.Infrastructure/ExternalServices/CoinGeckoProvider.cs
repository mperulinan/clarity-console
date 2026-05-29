using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Portfolio.Infrastructure.ExternalServices;

public class CoinGeckoProvider(
    HttpClient httpClient, 
    IConfiguration configuration,
    ILogger<CoinGeckoProvider> logger)
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
            logger.LogDebug("Fetching prices for {Count} assets from CoinGecko...", ids.Count);
            var response = await _httpClient.GetFromJsonAsync<Dictionary<string, Dictionary<string, decimal>>>(url);
            if (response == null) 
            {
                logger.LogWarning("CoinGecko simple/price returned null for context {Ids}", idsParam);
                return [];
            }

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
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch prices from CoinGecko for assets: {Ids}", idsParam);
            return [];
        }
    }

    public record CoinGeckoHistoryResponseDto(
        [property: JsonPropertyName("market_data")] CoinGeckoHistoryMarketDataDto? MarketData
    );

    public record CoinGeckoHistoryMarketDataDto(
        [property: JsonPropertyName("current_price")] Dictionary<string, decimal>? CurrentPrice
    );

    public async Task<decimal?> GetHistoricalPriceAsync(string externalId, FiatCurrency currency, DateTime date)
    {
        string dateStr = date.ToString("dd-MM-yyyy");
        string vsCurrency = currency.Value.ToLower();
        string url = $"{_baseUrl}coins/{externalId.ToLower()}/history?date={dateStr}&localization=false&x_cg_demo_api_key={_apiKey}";

        try
        {
            logger.LogDebug("Fetching historical price for {Id} at {Date} from CoinGecko...", externalId, dateStr);
            var response = await _httpClient.GetFromJsonAsync<CoinGeckoHistoryResponseDto>(url);
            
            if (response?.MarketData?.CurrentPrice != null && response.MarketData.CurrentPrice.TryGetValue(vsCurrency, out var price))
            {
                return price;
            }

            logger.LogWarning("CoinGecko history returned no price for {Id} at {Date}", externalId, dateStr);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch historical price from CoinGecko for asset: {Id} at {Date}", externalId, dateStr);
            return null;
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
            logger.LogDebug("Fetching market assets from: {Url}", url);
            var dtos = await _httpClient.GetFromJsonAsync<List<CoinGeckoMarketDto>>(url);
            if (dtos == null) 
            {
                logger.LogWarning("CoinGecko markets returned null result.");
                return [];
            }

            return dtos.Select(d => new Asset(
                d.Symbol.ToUpper(),
                d.Name,
                d.Id,
                d.ImageUrl,
                AssetType.Crypto
            ));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching market assets from CoinGecko.");
            return [];
        }
    }

    public record CoinGeckoSearchItemDto(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("symbol")] string Symbol,
        [property: JsonPropertyName("large")] string? ImageUrl,
        [property: JsonPropertyName("market_cap_rank")] int? MarketCapRank
    );

    public record CoinGeckoSearchResponseDto(
        [property: JsonPropertyName("coins")] List<CoinGeckoSearchItemDto> Coins
    );

    public async Task<IEnumerable<SearchAssetResult>> SearchAssetsAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        string url = $"{_baseUrl}search?query={Uri.EscapeDataString(query)}&x_cg_demo_api_key={_apiKey}";

        try
        {
            logger.LogInformation("Searching for asset '{Query}' on CoinGecko...", query);
            var response = await _httpClient.GetFromJsonAsync<CoinGeckoSearchResponseDto>(url);
            if (response?.Coins == null) 
            {
                logger.LogWarning("CoinGecko search returned empty result for '{Query}'", query);
                return [];
            }

            return response.Coins.Select(c => new SearchAssetResult(
                new Asset(
                    c.Symbol.ToUpper(),
                    c.Name,
                    c.Id,
                    c.ImageUrl,
                    AssetType.Crypto
                ),
                c.MarketCapRank
            ));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Asset Search failed on CoinGecko for query '{Query}'", query);
            return [];
        }
    }
}
