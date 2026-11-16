using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Portfolio.Application.Interfaces;
using Portfolio.Application.Models;

namespace Portfolio.Infrastructure.ImportParsers;

/// <summary>
/// Handles CSV stream lifecycle, header validation, and record iteration for DEGIRO (Spanish) imports.
/// Automatically detects Trade exports ("Transacciones") versus Account statement exports ("Estado de cuenta")
/// and delegates row-level parsing to <see cref="IDegiroRowParser"/> or <see cref="IDegiroAccountRowParser"/>.
/// </summary>
public class DegiroTransactionParser : ITransactionImportParser
{
    private readonly IDegiroRowParser _rowParser;
    private readonly IDegiroAccountRowParser _accountRowParser;

    public DegiroTransactionParser()
    {
        _rowParser = new DegiroRowParser();
        _accountRowParser = new DegiroAccountRowParser();
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
            return [];
        }

        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? throw new InvalidOperationException("The uploaded file does not match the expected DEGIRO (Spanish) CSV format.");

        var isAccountStatement = IsValidDegiroAccountHeader(headers);
        var isTradeStatement = IsValidDegiroTradeHeader(headers);

        if (!isAccountStatement && !isTradeStatement)
        {
            throw new InvalidOperationException("The uploaded file does not match the expected DEGIRO (Spanish) CSV format.");
        }

        var rows = new List<ImportedTransactionRow>();

        while (await csv.ReadAsync())
        {
            if (isAccountStatement)
            {
                if (_accountRowParser.TryParseRow(csv, headers, out var row))
                {
                    rows.Add(row);
                }
            }
            else
            {
                if (_rowParser.TryParseRow(csv, headers, out var row))
                {
                    rows.Add(row);
                }
            }
        }

        return rows;
    }

    private static bool IsValidDegiroTradeHeader(string[] headers)
    {
        return DegiroHeaders.Required.All(req => headers.Any(h => string.Equals(h?.Trim(), req, StringComparison.OrdinalIgnoreCase)));
    }

    private static bool IsValidDegiroAccountHeader(string[] headers)
    {
        return DegiroHeaders.AccountRequired.All(req => headers.Any(h => string.Equals(h?.Trim(), req, StringComparison.OrdinalIgnoreCase)));
    }
}
