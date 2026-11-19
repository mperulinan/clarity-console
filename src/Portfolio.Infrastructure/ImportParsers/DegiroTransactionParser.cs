using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Portfolio.Application.Interfaces;
using Portfolio.Application.Models;

namespace Portfolio.Infrastructure.ImportParsers;

/// <summary>
/// Handles CSV stream lifecycle, header validation, and record iteration for DEGIRO (Spanish) imports.
/// Automatically detects Trade exports ("Transacciones") versus Account statement exports ("Estado de cuenta")
/// and delegates row-level parsing to the corresponding <see cref="IDegiroRowParser"/> implementation.
/// </summary>
public class DegiroTransactionParser(ILogger<DegiroTransactionParser>? logger = null) : ITransactionImportParser
{
    private readonly ILogger<DegiroTransactionParser> _logger = logger ?? NullLogger<DegiroTransactionParser>.Instance;
    private readonly IDegiroRowParser _tradeRowParser = new DegiroRowParser();
    private readonly IDegiroRowParser _accountRowParser = new DegiroAccountRowParser();

    public string BrokerKey => "degiro";
    public string DisplayName => "DEGIRO";

    public async Task<ImportParseResult> ParseAsync(
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
            return ImportParseResult.Empty;
        }

        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? throw new InvalidOperationException("The uploaded file does not match the expected DEGIRO (Spanish) CSV format.");

        var isAccountStatement = IsValidDegiroAccountHeader(headers);
        var isTradeStatement = IsValidDegiroTradeHeader(headers);

        if (!isAccountStatement && !isTradeStatement)
        {
            throw new InvalidOperationException("The uploaded file does not match the expected DEGIRO (Spanish) CSV format.");
        }

        var rowParser = isAccountStatement ? _accountRowParser : _tradeRowParser;
        var rows = new List<ImportedTransactionRow>();
        var warnings = new List<string>();
        var skippedCount = 0;
        var rowIndex = 1;

        while (await csv.ReadAsync())
        {
            rowIndex++;
            if (rowParser.TryParseRow(csv, headers, out var row))
            {
                rows.Add(row);
            }
            else
            {
                skippedCount++;
                var rawRecord = csv.Parser.RawRecord?.TrimEnd('\r', '\n');
                warnings.Add($"Row {rowIndex} could not be parsed or was ignored.");
                _logger.LogWarning("DEGIRO import skipped row {RowIndex}: {RawRecord}", rowIndex, rawRecord);
            }
        }

        if (skippedCount > 0)
        {
            _logger.LogInformation("DEGIRO import parsed {ParsedCount} transaction(s), skipped {SkippedCount} row(s).",
                rows.Count, skippedCount);
        }

        return new ImportParseResult(rows, skippedCount, warnings);
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
