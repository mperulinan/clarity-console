using Portfolio.Application.DTOs;
using Portfolio.Application.Models;
using Portfolio.Application.Services;
using Portfolio.Application.Tests.Fakes;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;

namespace Portfolio.Application.Tests.Services;

public class ImportPreviewClassifierTests
{
    private readonly FakeUnitOfWork _uow;
    private readonly ImportPreviewClassifier _sut;

    public ImportPreviewClassifierTests()
    {
        _uow = new FakeUnitOfWork();
        _sut = new ImportPreviewClassifier(_uow);
    }

    [Fact]
    public async Task ClassifyRowsAsync_ExistingBrokerId_MarksAsDuplicate()
    {
        // Arrange
        var row = new ImportedTransactionRow(
            DateTime.UtcNow, "Apple Inc", "US0378331005", "NSY", 10m, 175m, "USD", 1.06m, 1m, -1650m, "dup-id"
        );

        var isinMap = new Dictionary<string, AssetDto?>(StringComparer.OrdinalIgnoreCase)
        {
            ["US0378331005"] = new AssetDto { Symbol = "AAPL", Name = "Apple Inc", ExternalId = "AAPL", Type = AssetType.Stock.Value }
        };

        var existingIds = new HashSet<string> { "dup-id" };

        // Act
        var result = await _sut.ClassifyRowsAsync([row], isinMap, existingIds);

        // Assert
        Assert.Single(result);
        var preview = result[0];
        Assert.True(preview.IsDuplicate);
        Assert.Equal("Transaction already imported.", preview.WarningMessage);
    }

    [Fact]
    public async Task ClassifyRowsAsync_ResolvedAssetExistsLocallyAsStock_SetsResolvedAssetIdAndType()
    {
        // Arrange
        var stock = new Asset("AAPL", "Apple Inc", "AAPL", null, AssetType.Stock);
        await _uow.Assets.AddAsync(stock);

        var row = new ImportedTransactionRow(
            DateTime.UtcNow, "Apple Inc", "US0378331005", "NSY", 10m, 175m, "USD", 1.06m, 1m, -1650m, "id-1"
        );

        var isinMap = new Dictionary<string, AssetDto?>(StringComparer.OrdinalIgnoreCase)
        {
            ["US0378331005"] = new AssetDto { Symbol = "AAPL", Name = "Apple Inc", ExternalId = "AAPL", Type = AssetType.Stock.Value }
        };

        // Act
        var result = await _sut.ClassifyRowsAsync([row], isinMap, new HashSet<string>());

        // Assert
        Assert.Single(result);
        var preview = result[0];
        Assert.Equal("AAPL", preview.ResolvedSymbol);
        Assert.Equal(stock.Id, preview.ResolvedAssetId);
        Assert.Equal(AssetType.Stock.Value, preview.ResolvedAssetType);
        Assert.False(preview.WillAutoCreate);
        Assert.False(preview.IsDuplicate);
        Assert.False(preview.IsUnresolvable);
    }

    [Fact]
    public async Task ClassifyRowsAsync_ResolvedAssetExistsLocallyAsEtf_SetsResolvedAssetIdAndType()
    {
        // Arrange
        var etf = new Asset("VWCE", "Vanguard FTSE All-World", "VWCE", null, AssetType.Etf);
        await _uow.Assets.AddAsync(etf);

        var row = new ImportedTransactionRow(
            DateTime.UtcNow, "Vanguard FTSE All-World", "IE00BK5BQT80", "EAM", 10m, 100m, "EUR", null, 1m, -1001m, "id-etf"
        );

        var isinMap = new Dictionary<string, AssetDto?>(StringComparer.OrdinalIgnoreCase)
        {
            ["IE00BK5BQT80"] = new AssetDto { Symbol = "VWCE", Name = "Vanguard FTSE All-World", ExternalId = "VWCE", Type = AssetType.Etf.Value }
        };

        // Act
        var result = await _sut.ClassifyRowsAsync([row], isinMap, new HashSet<string>());

        // Assert
        Assert.Single(result);
        var preview = result[0];
        Assert.Equal("VWCE", preview.ResolvedSymbol);
        Assert.Equal(etf.Id, preview.ResolvedAssetId);
        Assert.Equal(AssetType.Etf.Value, preview.ResolvedAssetType);
        Assert.False(preview.WillAutoCreate);
        Assert.False(preview.IsDuplicate);
        Assert.False(preview.IsUnresolvable);
    }

    [Fact]
    public async Task ClassifyRowsAsync_ResolvedAssetNotLocal_SetsWillAutoCreate()
    {
        // Arrange
        var row = new ImportedTransactionRow(
            DateTime.UtcNow, "Apple Inc", "US0378331005", "NSY", 10m, 175m, "USD", 1.06m, 1m, -1650m, "id-1"
        );

        var isinMap = new Dictionary<string, AssetDto?>(StringComparer.OrdinalIgnoreCase)
        {
            ["US0378331005"] = new AssetDto { Symbol = "AAPL", Name = "Apple Inc", ExternalId = "AAPL", Type = AssetType.Stock.Value }
        };

        // Act
        var result = await _sut.ClassifyRowsAsync([row], isinMap, new HashSet<string>());

        // Assert
        Assert.Single(result);
        var preview = result[0];
        Assert.Equal("AAPL", preview.ResolvedSymbol);
        Assert.Null(preview.ResolvedAssetId);
        Assert.True(preview.WillAutoCreate);
        Assert.Equal(AssetType.Stock.Value, preview.ResolvedAssetType);
        Assert.False(preview.IsUnresolvable);
    }

    [Fact]
    public async Task ClassifyRowsAsync_NullInIsinMap_MarksIsUnresolvable()
    {
        // Arrange
        var row = new ImportedTransactionRow(
            DateTime.UtcNow, "Unknown Stock", "ES0100000000", "BME", 10m, 10m, "EUR", null, 1m, -101m, "id-unknown"
        );

        var isinMap = new Dictionary<string, AssetDto?>(StringComparer.OrdinalIgnoreCase)
        {
            ["ES0100000000"] = null
        };

        // Act
        var result = await _sut.ClassifyRowsAsync([row], isinMap, new HashSet<string>());

        // Assert
        Assert.Single(result);
        var preview = result[0];
        Assert.True(preview.IsUnresolvable);
        Assert.Equal("Could not resolve ticker symbol for ISIN 'ES0100000000'.", preview.WarningMessage);
    }
}

