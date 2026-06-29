using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;

namespace Portfolio.Infrastructure.ExternalServices;

public class FrankfurterExchangeRateProvider(
    HttpClient httpClient, 
    IConfiguration configuration,
    ILogger<FrankfurterExchangeRateProvider> logger,
    TimeProvider? timeProvider = null) : IExchangeRateProvider
{
    private readonly string _baseUrl = (configuration["Frankfurter:BaseUrl"] ?? "https://api.frankfurter.app/").TrimEnd('/') + "/";
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public async Task<decimal> GetExchangeRateAsync(FiatCurrency from, FiatCurrency to) =>
        await GetRateAsync("latest", from.Symbol, to.Symbol);

    public Task<decimal> GetUsdEurRateAsync(DateTime date) =>
        GetHistoricalRateAsync(date, FiatCurrency.USD.Symbol, FiatCurrency.EUR.Symbol);

    public Task<decimal> GetEurUsdRateAsync(DateTime date) =>
        GetHistoricalRateAsync(date, FiatCurrency.EUR.Symbol, FiatCurrency.USD.Symbol);

    private async Task<decimal> GetHistoricalRateAsync(DateTime date, string from, string to)
    {
        if (date.Date >= _timeProvider.GetUtcNow().Date)
            throw new NotSupportedException("Exchange rates for the current or future dates are not available yet.");

        return await GetRateAsync(date.ToString("yyyy-MM-dd"), from, to);
    }

    private async Task<decimal> GetRateAsync(string path, string from, string to)
    {
        if (from.Equals(to, StringComparison.OrdinalIgnoreCase)) return 1.0m;

        var url = $"{_baseUrl}{path}?from={from.ToUpperInvariant()}&to={to.ToUpperInvariant()}";

        try
        {
            logger.LogDebug("Fetching exchange rate {From}->{To} for {Path} from Frankfurter...", from, to, path);
            var response = await httpClient.GetFromJsonAsync<FrankfurterResponse>(url);
            
            if (response?.Rates != null && response.Rates.TryGetValue(to.ToUpperInvariant(), out var rate))
            {
                logger.LogInformation("Exchange rate {From}->{To} for {Date} is {Rate}", from, to, response.Date, rate);
                return rate;
            }

            logger.LogWarning("Frankfurter response for {Url} did not contain the expected rate.", url);
            throw new InvalidOperationException($"{to} rate not found in Frankfurter response.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch exchange rate {From}->{To} for {Path} from Frankfurter.", from, to, path);
            throw new InvalidOperationException($"Failed to fetch exchange rate {from}->{to} for {path}: {ex.Message}", ex);
        }
    }

    private record FrankfurterResponse(decimal Amount, string Base, string Date, Dictionary<string, decimal> Rates);
}
