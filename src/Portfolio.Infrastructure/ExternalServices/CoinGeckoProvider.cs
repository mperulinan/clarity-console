using Microsoft.Extensions.Configuration;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;
using System.Text.Json;

namespace Portfolio.Infrastructure.ExternalServices;

public class CoinGeckoProvider(HttpClient httpClient, IConfiguration configuration)
    : ICryptoPriceProvider, IAssetMetadataProvider
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly string _baseUrl = configuration["CoinGecko:BaseUrl"] ?? "https://api.coingecko.com/api/v3/";
    private readonly string? _apiKey = configuration["CoinGecko:ApiKey"];

    // ── ICryptoPriceProvider ─────────────────────────────────────────────────

    public async Task<Dictionary<string, decimal>> GetCurrentCryptoPricesAsync(IEnumerable<string> assetIds, FiatCurrency priceCurrency)
    {
        var result = new Dictionary<string, decimal>();

        var cryptoIds = assetIds
            .Where(id => !string.Equals(id, FiatCurrency.USD.Value, StringComparison.OrdinalIgnoreCase)
                      && !string.Equals(id, FiatCurrency.EUR.Value, StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .ToList();

        string strCurrency = priceCurrency.Value;

        if (cryptoIds.Count == 0)
        {
            return result;
        }

        try
        {
            var idsParam = string.Join(",", cryptoIds.Select(id => id.ToLower()));
            string url = $"{_baseUrl}simple/price?ids={idsParam}&vs_currencies={strCurrency}&x_cg_demo_api_key={_apiKey}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return result;
            }

            string json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            foreach (var id in cryptoIds)
            {
                var lowerId = id.ToLower();
                if (doc.RootElement.TryGetProperty(lowerId, out var coinElement) &&
                    coinElement.TryGetProperty(strCurrency, out var priceElement))
                {
                    result[id] = priceElement.GetDecimal();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching CoinGecko prices: {ex.Message}");
        }

        return result;
    }

    // ── IAssetMetadataProvider ───────────────────────────────────────────────

    public async Task<Dictionary<string, string>> GetAssetImageUrlsAsync(IEnumerable<string> assetIds)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var cryptoIds = assetIds
            .Where(id => !string.Equals(id, FiatCurrency.USD.Value, StringComparison.OrdinalIgnoreCase)
                      && !string.Equals(id, FiatCurrency.EUR.Value, StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .ToList();

        if (cryptoIds.Count == 0)
        {
            return result;
        }

        try
        {
            var idsParam = string.Join(",", cryptoIds.Select(id => id.ToLower()));
            // /coins/markets returns coin metadata including the canonical image URL per coin ID
            string url = $"{_baseUrl}coins/markets?vs_currency=usd&ids={idsParam}&x_cg_demo_api_key={_apiKey}";

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return result;
            }

            string json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            foreach (var element in doc.RootElement.EnumerateArray())
            {
                if (element.TryGetProperty("id", out var idProp) &&
                    element.TryGetProperty("image", out var imageProp))
                {
                    var id = idProp.GetString();
                    var imageUrl = imageProp.GetString();
                    if (id is not null && imageUrl is not null)
                    {
                        result[id] = imageUrl;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching CoinGecko image metadata: {ex.Message}");
        }

        return result;
    }
}
