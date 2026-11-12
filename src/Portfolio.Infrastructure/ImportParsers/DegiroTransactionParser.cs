using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Portfolio.Application.Interfaces;
using Portfolio.Application.Models;

namespace Portfolio.Infrastructure.ImportParsers;

/// <summary>
/// Handles CSV stream lifecycle, header validation, and record iteration for DEGIRO (Spanish) imports.
/// Delegates row-level parsing and conversions to <see cref="IDegiroRowParser"/>.
/// </summary>
public class DegiroTransactionParser : ITransactionImportParser
{
    private readonly IDegiroRowParser _rowParser;

    public DegiroTransactionParser() : this(new DegiroRowParser()) { }

    internal DegiroTransactionParser(IDegiroRowParser rowParser)
    {
        _rowParser = rowParser;
    }

    public string BrokerKey => "degiro";
    public string DisplayName => "DEGIRO";

    public async Task<IReadOnlyList<ImportedTransactionRow>> ParseAsync(
        Stream csvStream, 
        CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(csvStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.Trim,
            MissingFieldFound = null,
            BadDataFound = null
        };

        using var csv = new CsvReader(reader, config);

        if (!await csv.ReadAsync())
        {
            return Array.Empty<ImportedTransactionRow>();
        }

        csv.ReadHeader();
        var headers = csv.HeaderRecord;
        if (headers == null || !IsValidDegiroSpanishHeader(headers))
        {
            throw new InvalidOperationException("The uploaded file does not match the expected DEGIRO (Spanish) CSV format.");
        }

        var rows = new List<ImportedTransactionRow>();

        while (await csv.ReadAsync())
        {
            if (_rowParser.TryParseRow(csv, headers, out var row))
            {
                rows.Add(row);
            }
        }

        return rows;
    }

    private static bool IsValidDegiroSpanishHeader(string[] headers)
    {
        return DegiroHeaders.Required.All(req => headers.Any(h => string.Equals(h?.Trim(), req, StringComparison.OrdinalIgnoreCase)));
    }
}
