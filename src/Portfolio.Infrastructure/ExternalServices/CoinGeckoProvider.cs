using Microsoft.Extensions.Configuration;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.ValueObjects;
using System.Text.Json;

namespace Portfolio.Infrastructure.ExternalServices;

public class CoinGeckoProvider(HttpClient httpClient, IConfiguration configuration)
    : ICryptoMarketDataProvider
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly string _baseUrl = configuration["CoinGecko:BaseUrl"] ?? "https://api.coingecko.com/api/v3/";
    private readonly string? _apiKey = configuration["CoinGecko:ApiKey"];

    public async Task<Dictionary<string, AssetMarketData>> GetCryptoMarketDataAsync(IEnumerable<string> assetIds, FiatCurrency priceCurrency)
    {
        var result = new Dictionary<string, AssetMarketData>(StringComparer.OrdinalIgnoreCase);

        var cryptoIds = assetIds.Distinct().ToList();
        
        if (cryptoIds.Count == 0)
        {
            return result;
        }

        string strCurrency = priceCurrency.Value.ToLower();

        try
        {
            var idsParam = string.Join(",", cryptoIds.Select(id => id.ToLower()));
            
            // Single API call to /coins/markets gets BOTH current_price and image thumbnail
            string url = $"{_baseUrl}coins/markets?vs_currency={strCurrency}&ids={idsParam}&x_cg_demo_api_key={_apiKey}";

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
                    element.TryGetProperty("current_price", out var priceProp))
                {
                    var id = idProp.GetString();
                    if (id == null) continue;

                    // Parse price
                    decimal price = 0;
                    if (priceProp.ValueKind == JsonValueKind.Number)
                    {
                        price = priceProp.GetDecimal();
                    }

                    // Parse image (nullable)
                    string? imageUrl = null;
                    if (element.TryGetProperty("image", out var imageProp) && imageProp.ValueKind == JsonValueKind.String)
                    {
                        imageUrl = imageProp.GetString();
                    }

                    result[id] = new AssetMarketData(price, imageUrl);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching CoinGecko market data: {ex.Message}");
        }

        return result;
    }
}
