using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Portfolio.Application.CQRS.Commands;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using Portfolio.Application.Models;
using Portfolio.Application.Tests.Fakes;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Application.Tests.CQRS.Commands;

public class PreviewImportTransactionsCommandHandlerTests
{
    private readonly ITransactionImportParser _parser;
    private readonly FakeUnitOfWork _uow;
    private readonly IAssetSearchService _searchService;
    private readonly IMemoryCache _cache;
    private readonly PreviewImportTransactionsCommandHandler _sut;

    public PreviewImportTransactionsCommandHandlerTests()
    {
        _parser = Substitute.For<ITransactionImportParser>();
        _parser.BrokerKey.Returns("degiro");

        _uow = new FakeUnitOfWork();
        _searchService = Substitute.For<IAssetSearchService>();

        var options = Substitute.For<IOptions<MemoryCacheOptions>>();
        options.Value.Returns(new MemoryCacheOptions());
        _cache = new MemoryCache(options);

        _sut = new PreviewImportTransactionsCommandHandler(
            [_parser],
            _uow,
            _searchService,
            _cache,
            NullLogger<PreviewImportTransactionsCommandHandler>.Instance
        );
    }

    [Fact]
    public async Task Handle_ValidNewTransactions_ReturnsPreviewWithTokenAndCachedSession()
    {
        // Arrange
        var rows = new List<ImportedTransactionRow>
        {
            new(DateTime.UtcNow, "Apple Inc", "US0378331005", "NSY", 10m, 175m, "USD", 1.06m, 1m, -1650m, "uuid-1"),
            new(DateTime.UtcNow, "Microsoft Corp", "US5949181045", "NDQ", 5m, 330m, "USD", 1.06m, 1m, -1550m, "uuid-2")
        };
        _parser.ParseAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>()).Returns(rows);

        _searchService.SearchAsync("US0378331005", Arg.Any<string>())
            .Returns([new AssetDto { Symbol = "AAPL", Name = "Apple Inc", ExternalId = "AAPL", Type = AssetType.Stock.Value }]);
        _searchService.SearchAsync("US5949181045", Arg.Any<string>())
            .Returns([new AssetDto { Symbol = "MSFT", Name = "Microsoft Corp", ExternalId = "MSFT", Type = AssetType.Stock.Value }]);

        using var stream = new MemoryStream();
        var command = new PreviewImportTransactionsCommand("degiro", stream);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.PreviewToken));
        Assert.Equal(2, result.TotalRows);
        Assert.Equal(0, result.DuplicateRows);
        Assert.Equal(0, result.UnresolvableRows);
        Assert.Equal(2, result.ReadyToImportRows);

        // Verify session stored in cache
        var cached = _cache.Get<ImportPreviewSession>(result.PreviewToken);
        Assert.NotNull(cached);
        Assert.Equal("degiro", cached.BrokerKey);
        Assert.Equal(2, cached.Rows.Count);
        Assert.Equal("AAPL", cached.Rows[0].ResolvedSymbol);
        Assert.True(cached.Rows[0].WillAutoCreate);
    }

    [Fact]
    public async Task Handle_ExistingTransaction_MarksRowAsDuplicate()
    {
        // Arrange
        var existingTx = new Transaction(
            DateTime.UtcNow.AddDays(-1),
            TransactionType.Swap,
            FiatCurrency.EUR.Id,
            Guid.NewGuid(),
            100m,
            1m,
            null,
            null,
            0m,
            FiatCurrency.EUR.Id,
            null,
            null,
            null,
            null,
            null,
            "existing",
            "uuid-dup"
        );
        await _uow.Transactions.AddAsync(existingTx);

        var rows = new List<ImportedTransactionRow>
        {
            new(DateTime.UtcNow, "Apple Inc", "US0378331005", "NSY", 10m, 175m, "USD", 1.06m, 1m, -1650m, "uuid-dup")
        };
        _parser.ParseAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>()).Returns(rows);

        using var stream = new MemoryStream();
        var command = new PreviewImportTransactionsCommand("degiro", stream);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.TotalRows);
        Assert.Equal(1, result.DuplicateRows);
        Assert.Equal(0, result.ReadyToImportRows);
        Assert.True(result.Rows[0].IsDuplicate);
    }

    [Fact]
    public async Task Handle_UnresolvableIsin_MarksRowAsUnresolvable()
    {
        // Arrange
        var rows = new List<ImportedTransactionRow>
        {
            new(DateTime.UtcNow, "Unknown Stock", "ES0100000000", "BME", 10m, 10m, "EUR", null, 1m, -101m, "uuid-unknown")
        };
        _parser.ParseAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>()).Returns(rows);
        _searchService.SearchAsync("ES0100000000", Arg.Any<string>()).Returns([]);

        using var stream = new MemoryStream();
        var command = new PreviewImportTransactionsCommand("degiro", stream);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.TotalRows);
        Assert.Equal(1, result.UnresolvableRows);
        Assert.Equal(0, result.ReadyToImportRows);
        Assert.True(result.Rows[0].IsUnresolvable);
    }

    [Fact]
    public async Task Handle_UnsupportedBrokerKey_ThrowsInvalidOperationException()
    {
        // Arrange
        using var stream = new MemoryStream();
        var command = new PreviewImportTransactionsCommand("unsupported_broker", stream);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.Handle(command, CancellationToken.None));
    }
}
