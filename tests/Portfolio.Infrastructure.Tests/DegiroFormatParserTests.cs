using Portfolio.Infrastructure.ImportParsers;

namespace Portfolio.Infrastructure.Tests;

public class DegiroFormatParserTests
{
    [Theory]
    [InlineData("20-10-2023", "14:30", 2023, 10, 20, 14, 30)]
    [InlineData("05-01-2022", "09:05", 2022, 1, 5, 9, 5)]
    public void ParseDateTime_ValidDateAndTimeString_ReturnsUtcDateTime(
        string fecha, string hora, int year, int month, int day, int hour, int minute)
    {
        var result = DegiroFormatParser.ParseDateTime(fecha, hora);

        Assert.NotNull(result);
        Assert.Equal(DateTimeKind.Utc, result.Value.Kind);
        Assert.Equal(new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Utc), result.Value);
    }

    [Fact]
    public void ParseDateTime_DateOnly_ReturnsMidnightUtc()
    {
        var result = DegiroFormatParser.ParseDateTime("15-03-2022", null);

        Assert.NotNull(result);
        Assert.Equal(new DateTime(2022, 3, 15, 0, 0, 0, DateTimeKind.Utc), result.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseDateTime_NullOrEmpty_ReturnsNull(string? fecha)
    {
        var result = DegiroFormatParser.ParseDateTime(fecha, "12:00");
        Assert.Null(result);
    }

    [Theory]
    [InlineData("175.50", 175.50)]
    [InlineData("175,50", 175.50)]
    [InlineData("-1652,45", -1652.45)]
    [InlineData("\"-1105,33\"", -1105.33)]
    [InlineData("100.0000", 100.0)]
    public void ParseDecimal_ValidStrings_ParsesCorrectly(string input, decimal expected)
    {
        var result = DegiroFormatParser.ParseDecimal(input);

        Assert.NotNull(result);
        Assert.Equal(expected, result.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseDecimal_EmptyOrWhitespace_ReturnsNull(string? input)
    {
        var result = DegiroFormatParser.ParseDecimal(input);
        Assert.Null(result);
    }
}

