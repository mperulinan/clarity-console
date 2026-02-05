using Microsoft.Extensions.Configuration;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;
using System.Text.Json;

namespace Portfolio.Infrastructure.ExternalServices;

public class CoinGeckoPriceProvider(HttpClient httpClient, IConfiguration configuration) : ICryptoPriceProvider
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly string _baseUrl = configuration["CoinGecko:BaseUrl"] ?? "https://api.coingecko.com/api/v3/";
    private readonly string? _apiKey = configuration["CoinGecko:ApiKey"];

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
}
