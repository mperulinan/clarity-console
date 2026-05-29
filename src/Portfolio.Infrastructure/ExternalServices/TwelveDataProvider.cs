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
    : IAssetPriceProvider, IAssetSearchProvider, IAssetCatalogProvider, IAssetLogoProvider
{
    private readonly string _baseUrl = configuration["TwelveData:BaseUrl"] ?? "https://api.twelvedata.com/";
    private readonly string? _apiKey = configuration["TwelveData:ApiKey"];

    // Maps Twelve Data's instrument_type string → our AssetType
    private static readonly Dictionary<string, AssetType> InstrumentTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Common Stock"]     = AssetType.Stock,
        ["ETF"]              = AssetType.Etf,
        ["Digital Currency"] = AssetType.Crypto,
    };

    private string GetApiUrl(string endpoint)
    {
        string separator = endpoint.Contains('?') ? "&" : "?";
        return $"{_baseUrl}{endpoint}{separator}apikey={_apiKey}";
    }

    // ── IAssetPriceProvider ──────────────────────────────────────────────

    public async Task<Dictionary<string, decimal>> GetPricesAsync(IEnumerable<string> externalIds, FiatCurrency currency)
    {
        var symbols = externalIds.Distinct().ToList();
        if (symbols.Count == 0) return [];

        // Twelve Data supports a comma-separated list of symbols in one call
        string symbolsParam = string.Join(",", symbols);
        string url = GetApiUrl($"price?symbol={Uri.EscapeDataString(symbolsParam)}");

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

    public async Task<decimal?> GetHistoricalPriceAsync(string externalId, FiatCurrency currency, DateTime date)
    {
        if (string.IsNullOrWhiteSpace(externalId)) return null;

        string endDate = date.ToString("yyyy-MM-dd");
        string url = GetApiUrl($"time_series?symbol={Uri.EscapeDataString(externalId)}&interval=1day&end_date={endDate}&outputsize=1");

        try
        {
            logger.LogDebug("Fetching historical price for {Symbol} on {Date} from Twelve Data...", externalId, endDate);
            var response = await httpClient.GetFromJsonAsync<TwelveDataTimeSeriesResponseDto>(url);

            if (response?.Status == "error")
            {
                logger.LogWarning("Twelve Data time_series returned error: {Message}", response.Message);
                return null;
            }

            var latestValue = response?.Values?.FirstOrDefault();
            if (latestValue?.Close != null && decimal.TryParse(latestValue.Close, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var price))
            {
                return price;
            }

            logger.LogWarning("Twelve Data time_series returned no values for {Symbol} on {Date}.", externalId, endDate);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch historical price from Twelve Data for symbol: {Symbol}", externalId);
            return null;
        }
    }

    // ── IAssetSearchProvider ─────────────────────────────────────────────

    public async Task<IEnumerable<SearchAssetResult>> SearchAssetsAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        var url = GetApiUrl($"symbol_search?symbol={Uri.EscapeDataString(query)}");

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
                        d.Name ?? d.InstrumentName ?? d.Symbol,
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

    // ── IAssetLogoProvider ──────────────────────────────────────────────

    public bool Supports(AssetType type) => type == AssetType.Stock || type == AssetType.Etf;

    public async Task<string?> GetLogoUrlAsync(string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol)) return null;

        string url = GetApiUrl($"logo?symbol={Uri.EscapeDataString(symbol)}");
        try
        {
            var response = await httpClient.GetFromJsonAsync<TwelveDataLogoDto>(url);
            return response?.Url;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to fetch logo for {Symbol} from TwelveData.", symbol);
            return null;
        }
    }

    // ── IAssetCatalogProvider ────────────────────────────────────────────

    public async Task<IEnumerable<Asset>> GetTopAssetsAsync(AssetType type, int count = 250)
    {
        if (type.IsCrypto() || (type != AssetType.Stock && type != AssetType.Etf))
            return [];

        var candidates = await FetchCandidatesAsync(type);

        return candidates
            .Where(c => !string.IsNullOrWhiteSpace(c.Symbol))
            .OrderBy(_ => Random.Shared.Next())
            .Take(count)
            .Select(c => new Asset(
                c.Symbol.ToUpper(),
                c.Name ?? c.InstrumentName ?? c.Symbol,
                externalId: c.Symbol.ToUpper(),
                imageUrl: null,
                type));
    }

    public Task<IEnumerable<Asset>> GetAssetsByExternalIdsAsync(AssetType type, IEnumerable<string> externalIds)
        => Task.FromResult<IEnumerable<Asset>>([]);

    /// <summary>
    /// Fetches the raw list of symbol candidates for the given asset type from the
    /// appropriate metadata endpoint (no quote credits consumed).
    /// </summary>
    private async Task<List<TwelveDataSearchItemDto>> FetchCandidatesAsync(AssetType type)
    {
        try
        {
            string url = type == AssetType.Stock
                ? GetApiUrl("stocks?exchange=NASDAQ&country=US&type=Common%20Stock")
                : GetApiUrl("etf?exchange=NYSE");

            var response = await httpClient.GetFromJsonAsync<TwelveDataSearchResponseDto>(url);
            return response?.Data ?? [];
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch symbol candidates from TwelveData for {Type}.", type.Name);
            return [];
        }
    }

    // ── DTOs ─────────────────────────────────────────────────────────────

    private record TwelveDataLogoDto(
        [property: JsonPropertyName("url")] string? Url
    );

    private record TwelveDataPriceDto(
        [property: JsonPropertyName("price")] string? Price
    );

    private record TwelveDataSearchItemDto(
        [property: JsonPropertyName("symbol")] string Symbol,
        [property: JsonPropertyName("instrument_name")] string? InstrumentName,
        [property: JsonPropertyName("name")] string? Name,
        [property: JsonPropertyName("exchange")] string? Exchange,
        [property: JsonPropertyName("type")] string? Type,
        [property: JsonPropertyName("instrument_type")] string? InstrumentType,
        [property: JsonPropertyName("country")] string? Country
    );

    private record TwelveDataSearchResponseDto(
        [property: JsonPropertyName("data")] List<TwelveDataSearchItemDto> Data
    );

    private record TwelveDataTimeSeriesResponseDto(
        [property: JsonPropertyName("status")] string? Status,
        [property: JsonPropertyName("message")] string? Message,
        [property: JsonPropertyName("values")] List<TwelveDataTimeSeriesValueDto>? Values
    );

    private record TwelveDataTimeSeriesValueDto(
        [property: JsonPropertyName("datetime")] string? Datetime,
        [property: JsonPropertyName("close")] string? Close
    );
}
