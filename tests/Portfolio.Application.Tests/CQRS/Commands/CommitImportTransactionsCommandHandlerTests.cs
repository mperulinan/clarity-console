using MediatR;
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
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Application.Tests.CQRS.Commands;

public class CommitImportTransactionsCommandHandlerTests
{
    private readonly FakeUnitOfWork _uow;
    private readonly IMediator _mediator;
    private readonly IBackgroundTaskQueue _backgroundQueue;
    private readonly IMemoryCache _cache;
    private readonly CommitImportTransactionsCommandHandler _sut;

    public CommitImportTransactionsCommandHandlerTests()
    {
        _uow = new FakeUnitOfWork();
        _mediator = Substitute.For<IMediator>();
        _backgroundQueue = Substitute.For<IBackgroundTaskQueue>();

        var options = Substitute.For<IOptions<MemoryCacheOptions>>();
        options.Value.Returns(new MemoryCacheOptions());
        _cache = new MemoryCache(options);

        _sut = new CommitImportTransactionsCommandHandler(
            _uow,
            _mediator,
            _backgroundQueue,
            _cache,
            NullLogger<CommitImportTransactionsCommandHandler>.Instance
        );
    }

    [Fact]
    public async Task Handle_ValidCachedPreview_PreSyncsAssetsAndPersistsTransactionsBatch()
    {
        // Arrange
        var token = "preview-token-123";
        var stockAssetId = Guid.NewGuid();
        var stockAsset = new Asset("AAPL", "Apple Inc", "AAPL", null, AssetType.Stock);
        typeof(Asset).GetProperty("Id")!.SetValue(stockAsset, stockAssetId);
        await _uow.Assets.AddAsync(stockAsset);

        var previewRows = new List<ImportedTransactionRowPreview>
        {
            new(
                Raw: new ImportedTransactionRow(DateTime.UtcNow.AddDays(-2), "Apple Inc", "US0378331005", "NSY", 10m, 175m, "USD", 1.06m, 1.5m, -1650m, "uuid-buy"),
                ResolvedSymbol: "AAPL",
                ResolvedAssetId: stockAssetId,
                WillAutoCreate: false,
                IsDuplicate: false,
                IsUnresolvable: false,
                WarningMessage: null
            ),
            new(
                Raw: new ImportedTransactionRow(DateTime.UtcNow.AddDays(-1), "Apple Inc", "US0378331005", "NSY", -5m, 180m, "USD", 1.06m, 1.5m, 850m, "uuid-sell"),
                ResolvedSymbol: "AAPL",
                ResolvedAssetId: stockAssetId,
                WillAutoCreate: false,
                IsDuplicate: false,
                IsUnresolvable: false,
                WarningMessage: null
            )
        };

        _cache.Set(token, new ImportPreviewSession("degiro", previewRows, DateTimeOffset.UtcNow));

        var command = new CommitImportTransactionsCommand("degiro", token);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.ImportedCount);
        Assert.Equal(0, result.SkippedCount);

        // Verify transactions added to repo and single SaveChanges call
        var savedTxs = (await _uow.Transactions.GetAllAsync()).ToList();
        Assert.Equal(2, savedTxs.Count);
        Assert.Equal(1, _uow.SaveChangesCallCount);

        // Verify buy tx: Swap EUR -> Stock
        var buyTx = savedTxs.First(t => t.BrokerTransactionId == "uuid-buy");
        Assert.Equal(FiatCurrency.EUR.Id, buyTx.FromAssetId);
        Assert.Equal(stockAssetId, buyTx.ToAssetId);
        Assert.Equal(1650m, buyTx.AmountSpent);
        Assert.Equal(10m, buyTx.AmountReceived);
        Assert.Equal(1.5m, buyTx.Fee);
        Assert.Equal(FiatCurrency.EUR.Id, buyTx.FeeAssetId);

        // Verify sell tx: Swap Stock -> EUR
        var sellTx = savedTxs.First(t => t.BrokerTransactionId == "uuid-sell");
        Assert.Equal(stockAssetId, sellTx.FromAssetId);
        Assert.Equal(FiatCurrency.EUR.Id, sellTx.ToAssetId);
        Assert.Equal(5m, sellTx.AmountSpent);
        Assert.Equal(850m, sellTx.AmountReceived);

        // Verify background jobs queued
        await _backgroundQueue.Received(2).QueueBackgroundWorkItemAsync(Arg.Any<SyncTransactionExchangeRateCommand>());

        // Verify cache evicted
        Assert.Null(_cache.Get<ImportPreviewSession>(token));
    }

    [Fact]
    public async Task Handle_WithExcludedBrokerIds_SkipsSpecifiedTransactions()
    {
        // Arrange
        var token = "preview-token-exclude";
        var stockAssetId = Guid.NewGuid();
        var stockAsset = new Asset("AAPL", "Apple Inc", "AAPL", null, AssetType.Stock);
        typeof(Asset).GetProperty("Id")!.SetValue(stockAsset, stockAssetId);
        await _uow.Assets.AddAsync(stockAsset);

        var previewRows = new List<ImportedTransactionRowPreview>
        {
            new(
                Raw: new ImportedTransactionRow(DateTime.UtcNow.AddDays(-2), "Apple Inc", "US0378331005", "NSY", 10m, 175m, "USD", 1.06m, 1m, -1650m, "uuid-keep"),
                ResolvedSymbol: "AAPL",
                ResolvedAssetId: stockAssetId,
                WillAutoCreate: false,
                IsDuplicate: false,
                IsUnresolvable: false,
                WarningMessage: null
            ),
            new(
                Raw: new ImportedTransactionRow(DateTime.UtcNow.AddDays(-1), "Apple Inc", "US0378331005", "NSY", 10m, 175m, "USD", 1.06m, 1m, -1650m, "uuid-skip"),
                ResolvedSymbol: "AAPL",
                ResolvedAssetId: stockAssetId,
                WillAutoCreate: false,
                IsDuplicate: false,
                IsUnresolvable: false,
                WarningMessage: null
            )
        };

        _cache.Set(token, new ImportPreviewSession("degiro", previewRows, DateTimeOffset.UtcNow));

        var command = new CommitImportTransactionsCommand("degiro", token, ["uuid-skip"]);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.ImportedCount);
        Assert.Equal(1, result.SkippedCount);

        var savedTxs = (await _uow.Transactions.GetAllAsync()).ToList();
        Assert.Single(savedTxs);
        Assert.Equal("uuid-keep", savedTxs[0].BrokerTransactionId);
    }

    [Fact]
    public async Task Handle_ExpiredOrMissingToken_ThrowsInvalidOperationException()
    {
        // Arrange
        var command = new CommitImportTransactionsCommand("degiro", "non-existent-token");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.Handle(command, CancellationToken.None));
    }
}
