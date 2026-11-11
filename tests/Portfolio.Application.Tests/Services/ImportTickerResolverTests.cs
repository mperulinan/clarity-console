using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using Portfolio.Application.Models;
using Portfolio.Application.Services;
using Portfolio.Domain.Enums;

namespace Portfolio.Application.Tests.Services;

public class ImportTickerResolverTests
{
    private readonly IAssetSearchService _searchService;
    private readonly ImportTickerResolver _sut;

    public ImportTickerResolverTests()
    {
        _searchService = Substitute.For<IAssetSearchService>();
        _sut = new ImportTickerResolver(_searchService, NullLogger<ImportTickerResolver>.Instance);
    }

    [Fact]
    public async Task ResolveIsinsAsync_ValidIsin_ReturnsMatchedAssetDto()
    {
        // Arrange
        var rows = new List<ImportedTransactionRow>
        {
            new(DateTime.UtcNow, "Apple Inc", "US0378331005", "NSY", 10m, 175m, "USD", 1.06m, 1m, -1650m, "id-1")
        };

        var expectedDto = new AssetDto
        {
            Symbol = "AAPL",
            Name = "Apple Inc",
            ExternalId = "AAPL",
            Type = AssetType.Stock.Value
        };

        _searchService.SearchAsync("US0378331005").Returns([expectedDto]);

        // Act
        var result = await _sut.ResolveIsinsAsync(rows);

        // Assert
        Assert.Single(result);
        Assert.True(result.ContainsKey("US0378331005"));
        Assert.Equal(expectedDto, result["US0378331005"]);
    }

    [Fact]
    public async Task ResolveIsinsAsync_IsinReturnsEmpty_FallsBackToProductName()
    {
        // Arrange
        var rows = new List<ImportedTransactionRow>
        {
            new(DateTime.UtcNow, "Apple Inc", "US0378331005", "NSY", 10m, 175m, "USD", 1.06m, 1m, -1650m, "id-1")
        };

        var fallbackDto = new AssetDto
        {
            Symbol = "AAPL",
            Name = "Apple Inc",
            ExternalId = "AAPL",
            Type = AssetType.Stock.Value
        };

        _searchService.SearchAsync("US0378331005").Returns([]);
        _searchService.SearchAsync("Apple Inc").Returns([fallbackDto]);

        // Act
        var result = await _sut.ResolveIsinsAsync(rows);

        // Assert
        Assert.Single(result);
        Assert.Equal(fallbackDto, result["US0378331005"]);
        await _searchService.Received(1).SearchAsync("Apple Inc");
    }

    [Fact]
    public async Task ResolveIsinsAsync_CandidateWithLocalTransactions_IsPreferred()
    {
        // Arrange
        var rows = new List<ImportedTransactionRow>
        {
            new(DateTime.UtcNow, "SAP SE", "DE0007164600", "XET", 5m, 150m, "EUR", null, 1m, -750m, "id-1")
        };

        var candidateWithoutTx = new AssetDto { Symbol = "SAP.DE", Name = "SAP SE XETRA", ExternalId = "1", TransactionCount = 0 };
        var candidateWithTx = new AssetDto { Symbol = "SAP", Name = "SAP SE NYSE", ExternalId = "2", TransactionCount = 3 };

        _searchService.SearchAsync("DE0007164600").Returns([candidateWithoutTx, candidateWithTx]);

        // Act
        var result = await _sut.ResolveIsinsAsync(rows);

        // Assert
        Assert.Single(result);
        Assert.Equal("SAP", result["DE0007164600"]?.Symbol);
    }

    [Fact]
    public async Task ResolveIsinsAsync_ExchangeHint_PrefersMatchingCandidateWhenNoPriorTransactions()
    {
        // Arrange
        var rows = new List<ImportedTransactionRow>
        {
            new(DateTime.UtcNow, "SAP SE", "DE0007164600", "XETRA", 5m, 150m, "EUR", null, 1m, -750m, "id-1")
        };

        var candidateOther = new AssetDto { Symbol = "SAP.US", Name = "SAP SE NYSE", ExternalId = "1", TransactionCount = 0 };
        var candidateMatch = new AssetDto { Symbol = "SAP.DE", Name = "SAP SE XETRA", ExternalId = "2", TransactionCount = 0 };

        _searchService.SearchAsync("DE0007164600").Returns([candidateOther, candidateMatch]);

        // Act
        var result = await _sut.ResolveIsinsAsync(rows);

        // Assert
        Assert.Single(result);
        Assert.Equal("SAP.DE", result["DE0007164600"]?.Symbol);
    }

    [Fact]
    public async Task ResolveIsinsAsync_SearchThrows_ReturnsNullForThatIsin()
    {
        // Arrange
        var rows = new List<ImportedTransactionRow>
        {
            new(DateTime.UtcNow, "Error Corp", "DE0000000000", "FRA", 1m, 10m, "EUR", null, 1m, -10m, "id-err")
        };

        _searchService.SearchAsync("DE0000000000").Throws(new HttpRequestException("Network failure"));

        // Act
        var result = await _sut.ResolveIsinsAsync(rows);

        // Assert
        Assert.Single(result);
        Assert.True(result.ContainsKey("DE0000000000"));
        Assert.Null(result["DE0000000000"]);
    }
}

