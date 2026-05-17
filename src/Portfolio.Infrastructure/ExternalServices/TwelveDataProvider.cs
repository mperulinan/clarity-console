using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Portfolio.Infrastructure.ExternalServices;

public class TwelveDataProvider(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<TwelveDataProvider> logger)
    : IAssetPriceProvider, IAssetSearchProvider
{
    private readonly string _baseUrl = configuration["TwelveData:BaseUrl"] ?? "https://api.twelvedata.com/";
    private readonly string? _apiKey = configuration["TwelveData:ApiKey"];

    // Maps Twelve Data's instrument_type string → our AssetType
    private static readonly Dictionary<string, AssetType> InstrumentTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Common Stock"]     = AssetType.Stock,
        ["ETF"]              = AssetType.Index,
        ["Digital Currency"] = AssetType.Crypto,
    };

    // ── IAssetPriceProvider ──────────────────────────────────────────────

    public async Task<Dictionary<string, decimal>> GetPricesAsync(IEnumerable<string> externalIds, FiatCurrency currency)
    {
        var symbols = externalIds.Distinct().ToList();
        if (symbols.Count == 0) return [];

        // Twelve Data supports a comma-separated list of symbols in one call
        string symbolsParam = string.Join(",", symbols);
        string url = $"{_baseUrl}price?symbol={Uri.EscapeDataString(symbolsParam)}&apikey={_apiKey}";

        try
        {
            logger.LogDebug("Fetching prices for {Count} symbols from Twelve Data...", symbols.Count);

            var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

            if (symbols.Count == 1)
            {
                // Single symbol: response is { "price": "123.45" }
                var single = await httpClient.GetFromJsonAsync<TwelveDataPriceDto>(url);
                if (single?.Price != null && decimal.TryParse(single.Price, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var price))
                {
                    result[symbols[0]] = price;
                }
            }
            else
            {
                // Multiple symbols: response is { "AAPL": { "price": "..." }, "SPY": { "price": "..." } }
                var multi = await httpClient.GetFromJsonAsync<Dictionary<string, TwelveDataPriceDto>>(url);
                if (multi != null)
                {
                    foreach (var kvp in multi)
                    {
                        if (kvp.Value?.Price != null && decimal.TryParse(kvp.Value.Price,
                                System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture, out var p))
                        {
                            result[kvp.Key] = p;
                        }
                    }
                }
            }

            // Note: Twelve Data returns prices in USD. Currency conversion is handled upstream.
            logger.LogDebug("Twelve Data returned prices for {Count}/{Total} symbols.", result.Count, symbols.Count);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch prices from Twelve Data for symbols: {Symbols}", symbolsParam);
            return [];
        }
    }

    // ── IAssetSearchProvider ─────────────────────────────────────────────

    public async Task<IEnumerable<SearchAssetResult>> SearchAssetsAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        var url = $"{_baseUrl}symbol_search?symbol={Uri.EscapeDataString(query)}&apikey={_apiKey}";

        try
        {
            logger.LogInformation("Searching Twelve Data for '{Query}'...", query);
            var response = await httpClient.GetFromJsonAsync<TwelveDataSearchResponseDto>(url);

            if (response?.Data == null)
            {
                logger.LogWarning("Twelve Data search returned empty result for '{Query}'", query);
                return [];
            }

            return response.Data
                .Where(d => !string.IsNullOrEmpty(d.Symbol)
                         && !string.IsNullOrEmpty(d.InstrumentName)
                         && InstrumentTypeMap.TryGetValue(d.InstrumentType ?? "", out _))
                .Select(d => new SearchAssetResult(
                    new Asset(
                        d.Symbol.ToUpper(),
                        d.InstrumentName,
                        externalId: d.Symbol.ToUpper(),
                        imageUrl: null,
                        InstrumentTypeMap[d.InstrumentType!]
                    ),
                    MarketCapRank: null
                ));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Asset search failed on Twelve Data for query '{Query}'", query);
            return [];
        }
    }

    // ── DTOs ─────────────────────────────────────────────────────────────

    private record TwelveDataPriceDto(
        [property: JsonPropertyName("price")] string? Price
    );

    private record TwelveDataSearchItemDto(
        [property: JsonPropertyName("symbol")] string Symbol,
        [property: JsonPropertyName("instrument_name")] string InstrumentName,
        [property: JsonPropertyName("exchange")] string? Exchange,
        [property: JsonPropertyName("instrument_type")] string? InstrumentType,
        [property: JsonPropertyName("country")] string? Country
    );

    private record TwelveDataSearchResponseDto(
        [property: JsonPropertyName("data")] List<TwelveDataSearchItemDto> Data
    );
}
