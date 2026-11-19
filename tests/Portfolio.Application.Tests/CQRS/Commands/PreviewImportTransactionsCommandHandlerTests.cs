using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Portfolio.Application.CQRS.Commands;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using Portfolio.Application.Models;
using Portfolio.Application.Tests.Fakes;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;

namespace Portfolio.Application.Tests.CQRS.Commands;

public class PreviewImportTransactionsCommandHandlerTests
{
    private readonly ITransactionImportParser _parser;
    private readonly FakeUnitOfWork _uow;
    private readonly IImportTickerResolver _tickerResolver;
    private readonly IImportPreviewClassifier _previewClassifier;
    private readonly FakeImportPreviewStore _previewStore;
    private readonly PreviewImportTransactionsCommandHandler _sut;

    public PreviewImportTransactionsCommandHandlerTests()
    {
        _parser = Substitute.For<ITransactionImportParser>();
        _parser.BrokerKey.Returns("degiro");

        _uow = new FakeUnitOfWork();
        _tickerResolver = Substitute.For<IImportTickerResolver>();
        _previewClassifier = Substitute.For<IImportPreviewClassifier>();
        _previewStore = new FakeImportPreviewStore();

        _sut = new PreviewImportTransactionsCommandHandler(
            [_parser],
            _uow,
            _tickerResolver,
            _previewClassifier,
            _previewStore,
            NullLogger<PreviewImportTransactionsCommandHandler>.Instance
        );
    }

    [Fact]
    public async Task Handle_ValidNewTransactions_ReturnsPreviewWithTokenAndCachedSession()
    {
        // Arrange
        var rows = new List<ImportedTransactionRow>
        {
            new(DateTime.UtcNow, "Apple Inc", "US0378331005", "NSY", 10m, 175m, "USD", 1.06m, 1m, -1650m, "USD", "uuid-1"),
            new(DateTime.UtcNow, "Microsoft Corp", "US5949181045", "NDQ", 5m, 330m, "USD", 1.06m, 1m, -1550m, "USD", "uuid-2")
        };
        _parser.ParseAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>()).Returns(new ImportParseResult(rows));

        var isinMap = new Dictionary<string, AssetDto?>(StringComparer.OrdinalIgnoreCase)
        {
            ["US0378331005"] = new AssetDto { Symbol = "AAPL", Name = "Apple Inc", ExternalId = "AAPL", Type = AssetType.Stock.Value },
            ["US5949181045"] = new AssetDto { Symbol = "MSFT", Name = "Microsoft Corp", ExternalId = "MSFT", Type = AssetType.Stock.Value }
        };
        _tickerResolver.ResolveIsinsAsync(Arg.Any<IEnumerable<ImportedTransactionRow>>(), Arg.Any<CancellationToken>())
            .Returns(isinMap);

        var previewRows = new List<ImportedTransactionRowPreview>
        {
            new(Raw: rows[0], ResolvedSymbol: "AAPL", ResolvedAssetId: null, WillAutoCreate: true,
                IsDuplicate: false, IsUnresolvable: false, WarningMessage: null, ResolvedAssetType: AssetType.Stock.Value),
            new(Raw: rows[1], ResolvedSymbol: "MSFT", ResolvedAssetId: null, WillAutoCreate: true,
                IsDuplicate: false, IsUnresolvable: false, WarningMessage: null, ResolvedAssetType: AssetType.Stock.Value)
        };
        _previewClassifier.ClassifyRowsAsync(Arg.Any<IReadOnlyList<ImportedTransactionRow>>(),
                Arg.Any<IReadOnlyDictionary<string, AssetDto?>>(),
                Arg.Any<IReadOnlySet<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(previewRows);

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
        var cached = _previewStore.Get(result.PreviewToken);
        Assert.NotNull(cached);
        Assert.Equal("degiro", cached.BrokerKey);
        Assert.Equal(2, cached.Rows.Count);
        Assert.Equal("AAPL", cached.Rows[0].ResolvedSymbol);
        Assert.Equal(AssetType.Stock.Value, cached.Rows[0].ResolvedAssetType);
        Assert.True(cached.Rows[0].WillAutoCreate);
    }

    [Fact]
    public async Task Handle_ValidTransactionResolvingEtf_PopulatesResolvedAssetTypeAsEtf()
    {
        // Arrange
        var rows = new List<ImportedTransactionRow>
        {
            new(DateTime.UtcNow, "Vanguard FTSE All-World", "IE00BK5BQT80", "EAM", 10m, 100m, "EUR", null, 1m, -1001m, "EUR", "uuid-etf-1")
        };
        _parser.ParseAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>()).Returns(new ImportParseResult(rows));

        _tickerResolver.ResolveIsinsAsync(Arg.Any<IEnumerable<ImportedTransactionRow>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, AssetDto?>(StringComparer.OrdinalIgnoreCase));

        var previewRows = new List<ImportedTransactionRowPreview>
        {
            new(Raw: rows[0], ResolvedSymbol: "VWCE", ResolvedAssetId: null, WillAutoCreate: true,
                IsDuplicate: false, IsUnresolvable: false, WarningMessage: null, ResolvedAssetType: AssetType.Etf.Value)
        };
        _previewClassifier.ClassifyRowsAsync(Arg.Any<IReadOnlyList<ImportedTransactionRow>>(),
                Arg.Any<IReadOnlyDictionary<string, AssetDto?>>(),
                Arg.Any<IReadOnlySet<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(previewRows);

        using var stream = new MemoryStream();
        var command = new PreviewImportTransactionsCommand("degiro", stream);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Rows);
        Assert.Equal("VWCE", result.Rows[0].ResolvedSymbol);
        Assert.Equal(AssetType.Etf.Value, result.Rows[0].ResolvedAssetType);

        var cached = _previewStore.Get(result.PreviewToken);
        Assert.NotNull(cached);
        Assert.Equal(AssetType.Etf.Value, cached.Rows[0].ResolvedAssetType);
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
            new(DateTime.UtcNow, "Apple Inc", "US0378331005", "NSY", 10m, 175m, "USD", 1.06m, 1m, -1650m, "USD", "uuid-dup")
        };
        _parser.ParseAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>()).Returns(new ImportParseResult(rows));

        _tickerResolver.ResolveIsinsAsync(Arg.Any<IEnumerable<ImportedTransactionRow>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, AssetDto?>(StringComparer.OrdinalIgnoreCase));

        var previewRows = new List<ImportedTransactionRowPreview>
        {
            new(Raw: rows[0], ResolvedSymbol: null, ResolvedAssetId: null, WillAutoCreate: false,
                IsDuplicate: true, IsUnresolvable: false, WarningMessage: "Transaction already imported.", ResolvedAssetType: null)
        };
        _previewClassifier.ClassifyRowsAsync(Arg.Any<IReadOnlyList<ImportedTransactionRow>>(),
                Arg.Any<IReadOnlyDictionary<string, AssetDto?>>(),
                Arg.Any<IReadOnlySet<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(previewRows);

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
            new(DateTime.UtcNow, "Unknown Stock", "ES0100000000", "BME", 10m, 10m, "EUR", null, 1m, -101m, "EUR", "uuid-unknown")
        };
        _parser.ParseAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>()).Returns(new ImportParseResult(rows));

        _tickerResolver.ResolveIsinsAsync(Arg.Any<IEnumerable<ImportedTransactionRow>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, AssetDto?>(StringComparer.OrdinalIgnoreCase));

        var previewRows = new List<ImportedTransactionRowPreview>
        {
            new(Raw: rows[0], ResolvedSymbol: null, ResolvedAssetId: null, WillAutoCreate: false,
                IsDuplicate: false, IsUnresolvable: true, WarningMessage: "Could not resolve ticker symbol for ISIN 'ES0100000000'.", ResolvedAssetType: null)
        };
        _previewClassifier.ClassifyRowsAsync(Arg.Any<IReadOnlyList<ImportedTransactionRow>>(),
                Arg.Any<IReadOnlyDictionary<string, AssetDto?>>(),
                Arg.Any<IReadOnlySet<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(previewRows);

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
