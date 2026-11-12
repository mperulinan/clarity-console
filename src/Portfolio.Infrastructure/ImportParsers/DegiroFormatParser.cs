using System.Globalization;

namespace Portfolio.Infrastructure.ImportParsers;

/// <summary>
/// Parses DEGIRO-specific date, time, and numeric representations (handling Spanish commas and Invariant fallbacks).
/// </summary>
internal static class DegiroFormatParser
{
    private static readonly CultureInfo SpanishCulture = new("es-ES");

    public static DateTime? ParseDateTime(string? fecha, string? hora)
    {
        if (string.IsNullOrWhiteSpace(fecha))
            return null;

        fecha = fecha.Trim();
        hora = hora?.Trim();

        if (!string.IsNullOrWhiteSpace(hora) &&
            DateTime.TryParseExact($"{fecha} {hora}", "dd-MM-yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsedDt))
        {
            return parsedDt;
        }

        if (DateTime.TryParseExact(fecha, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsedDateOnly))
        {
            return parsedDateOnly;
        }

        if (DateTime.TryParse(fecha, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var parsedGeneral))
        {
            return DateTime.SpecifyKind(parsedGeneral, DateTimeKind.Utc);
        }

        return null;
    }

    public static decimal? ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim().Trim('"', '\'');

        // Spanish DEGIRO exports use commas as decimal separators (e.g. "168,5600" or "-1105,33").
        // Parse with Spanish culture first, because InvariantCulture treats comma as thousands separator.
        if (trimmed.Contains(','))
        {
            if (decimal.TryParse(trimmed, NumberStyles.Any, SpanishCulture, out var parsedEs))
                return parsedEs;
        }

        if (decimal.TryParse(trimmed, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            return parsed;

        if (decimal.TryParse(trimmed, NumberStyles.Any, SpanishCulture, out var fallbackEs))
            return fallbackEs;

        return null;
    }
}

