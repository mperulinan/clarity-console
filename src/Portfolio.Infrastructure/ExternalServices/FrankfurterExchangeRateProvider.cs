using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Portfolio.Domain.Constants;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;

namespace Portfolio.Infrastructure.ExternalServices;

public class FrankfurterExchangeRateProvider(HttpClient httpClient, IConfiguration configuration) : IExchangeRateProvider
{
    private readonly string _baseUrl = (configuration["Frankfurter:BaseUrl"] ?? "https://api.frankfurter.app/").TrimEnd('/') + "/";

    public async Task<decimal> GetExchangeRateAsync(FiatCurrency from, FiatCurrency to) =>
        await GetRateAsync("latest", MapCurrency(from), MapCurrency(to));

    public async Task<decimal> GetUsdEurRateAsync(DateTime date) =>
        await GetRateAsync(date.ToString("yyyy-MM-dd"), CurrencyConstants.Usd, CurrencyConstants.Eur);

    public async Task<decimal> GetEurUsdRateAsync(DateTime date) =>
        await GetRateAsync(date.ToString("yyyy-MM-dd"), CurrencyConstants.Eur, CurrencyConstants.Usd);

    private async Task<decimal> GetRateAsync(string path, string from, string to)
    {
        if (from == to) return 1.0m;

        var url = $"{_baseUrl}{path}?from={from.ToUpper()}&to={to.ToUpper()}";

        try
        {
            var response = await httpClient.GetFromJsonAsync<FrankfurterResponse>(url);
            
            if (response?.Rates != null && response.Rates.TryGetValue(to.ToUpper(), out var rate))
            {
                return rate;
            }

            throw new InvalidOperationException($"{to} rate not found in Frankfurter response.");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to fetch exchange rate {from}->{to} for {path}: {ex.Message}", ex);
        }
    }

    private static string MapCurrency(FiatCurrency currency) =>
        currency == FiatCurrency.EUR ? CurrencyConstants.Eur : CurrencyConstants.Usd;

    private record FrankfurterResponse(decimal Amount, string Base, string Date, Dictionary<string, decimal> Rates);
}
