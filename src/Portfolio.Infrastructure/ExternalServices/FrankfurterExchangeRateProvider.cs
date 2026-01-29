using System.Text.Json;
using Portfolio.Domain.Constants;
using Portfolio.Domain.Interfaces;

namespace Portfolio.Infrastructure.ExternalServices;

public class FrankfurterExchangeRateProvider(HttpClient httpClient) : IExchangeRateProvider
{
    public async Task<decimal> GetUsdEurRateAsync(DateTime date)
    {
        // Frankfurter API format: https://api.frankfurter.app/YYYY-MM-DD?from=USD&to=EUR
        var dateStr = date.ToString("yyyy-MM-dd");
        // Using ToUpper() on constants to match API expectations
        var from = CurrencyConstants.Usd.ToUpper();
        var to = CurrencyConstants.Eur.ToUpper();
        var url = $"https://api.frankfurter.app/{dateStr}?from={from}&to={to}";

        try
        {
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            
            // Expected JSON: { "rates": { "EUR": 0.92 } }
            if (doc.RootElement.TryGetProperty("rates", out var ratesElement) &&
                ratesElement.TryGetProperty(to, out var eurRateElement))
            {
                return eurRateElement.GetDecimal();
            }

            throw new Exception("EUR rate not found in Frankfurter response");
        }
        catch (Exception ex)
        {
            // Fallback or rethrow? For now rethrow as it's critical for the calculation
            throw new Exception($"Failed to fetch exchange rate for {dateStr}: {ex.Message}", ex);
        }
    }
    public async Task<decimal> GetEurUsdRateAsync(DateTime date)
    {
        // Frankfurter API format: https://api.frankfurter.app/YYYY-MM-DD?from=EUR&to=USD
        var dateStr = date.ToString("yyyy-MM-dd");
        var from = CurrencyConstants.Eur.ToUpper();
        var to = CurrencyConstants.Usd.ToUpper();
        var url = $"https://api.frankfurter.app/{dateStr}?from={from}&to={to}";

        try
        {
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            
            if (doc.RootElement.TryGetProperty("rates", out var ratesElement) &&
                ratesElement.TryGetProperty(to, out var usdRateElement))
            {
                return usdRateElement.GetDecimal();
            }

            throw new Exception("USD rate not found in Frankfurter response");
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to fetch exchange rate (EUR->USD) for {dateStr}: {ex.Message}", ex);
        }
    }
}
