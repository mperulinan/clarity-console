using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Portfolio.Application.DTOs;
using Portfolio.Application.Services;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;

namespace Portfolio.Application.Tests.Services;

public class AssetSearchServiceTests
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAssetRepository _assetRepository;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAssetSearchProvider _searchProvider;
    private readonly AssetSearchService _sut;

    public AssetSearchServiceTests()
    {
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _assetRepository = Substitute.For<IAssetRepository>();
        _transactionRepository = Substitute.For<ITransactionRepository>();

        _unitOfWork.Assets.Returns(_assetRepository);
        _unitOfWork.Transactions.Returns(_transactionRepository);

        _searchProvider = Substitute.For<IAssetSearchProvider>();

        _sut = new AssetSearchService(
            _unitOfWork,
            [_searchProvider],
            new NullLogger<AssetSearchService>()
        );
    }

    [Fact]
    public async Task SearchAsync_EmptyQuery_ReturnsEmptyList()
    {
        // Arrange
        _assetRepository.GetAllAsync().Returns([]);
        _transactionRepository.GetAllAsync().Returns([]);

        // Act
        IEnumerable<AssetDto> results = await _sut.SearchAsync("");

        // Assert
        Assert.Empty(results);
        await _searchProvider.DidNotReceive().SearchAssetsAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task SearchAsync_ShortQuery_OnlySearchesLocal()
    {
        // Arrange
        string query = "B"; // Length < 2 (ExternalSearchMinLength)
        Asset localAsset = new Asset("BTC", "Bitcoin", externalId: null, imageUrl: null, AssetType.Crypto);
        _assetRepository.GetAllAsync().Returns([localAsset]);
        _transactionRepository.GetAllAsync().Returns([]);

        // Act
        IEnumerable<AssetDto> results = await _sut.SearchAsync(query);

        // Assert
        Assert.Single(results);
        await _searchProvider.DidNotReceive().SearchAssetsAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task SearchAsync_ComplexScenario_ReturnsCorrectTierOrder()
    {
        // Arrange
        string query = "ADA";

        // Local assets
        Asset localExactWithTx = new("ADA", "Cardano Local", "ext-ada-1", null, AssetType.Crypto); // Tier 1 (exact, tx>0)
        Asset localNonExactWithTx = new("ADAX", "Adax Local", null, null, AssetType.Crypto); // Tier 1 (non-exact, tx>0)
        Asset localExactNoTx = new("ADA", "Cardano NoTx", null, null, AssetType.Crypto); // Tier 2 (exact, tx=0)
        Asset localNonExactNoTx = new("ADAB", "AdaB Local", null, null, AssetType.Crypto); // Tier 3 (non-exact, tx=0)

        _assetRepository.GetAllAsync().Returns([localExactWithTx, localNonExactWithTx, localExactNoTx, localNonExactNoTx]);

        // Transactions to give counts
        // 2 txs for localExactWithTx, 1 tx for localNonExactWithTx
        Transaction tx1 = new(
            DateTime.UtcNow, TransactionType.Swap, localExactWithTx.Id, localNonExactWithTx.Id,
            10, 100, 1m, null, 1, null, null, null, null, FiatCurrency.USD, null, null);

        Transaction tx2 = new(
            DateTime.UtcNow, TransactionType.Reward, null, localExactWithTx.Id,
            0, 5, 50m, null, 0, null, null, null, null, FiatCurrency.USD, null, null);
        _transactionRepository.GetAllAsync().Returns([tx1, tx2]);

        // External assets
        Asset externalExact = new("ADA", "Cardano External", "ext-ada-2", null, AssetType.Crypto); // Tier 2
        Asset externalNonExactRank1 = new("ADAC", "AdaC External", "ext-adac", null, AssetType.Crypto); // Tier 4 (rank 1)
        Asset externalNonExactRank10 = new("ADAD", "AdaD External", "ext-adad", null, AssetType.Crypto); // Tier 4 (rank 10)

        // This one should be ignored because its external ID matches a local asset's external ID
        Asset externalDuplicate = new("ADA", "Cardano Duplicate", "ext-ada-1", null, AssetType.Crypto);

        List<SearchAssetResult> externalResults =
        [
            new(externalExact, 5), // Rank 5
            new(externalNonExactRank10, 10), // Rank 10
            new(externalNonExactRank1, 1), // Rank 1
            new(externalDuplicate, 2) // Should be skipped
        ];

        _searchProvider.SearchAssetsAsync(query).Returns(externalResults);

        // Act
        List<AssetDto> results = [.. (await _sut.SearchAsync(query))];

        // Assert
        Assert.Equal(7, results.Count); // 4 local + 4 external - 1 duplicate = 7

        // Tier 1: Local with transactions (ordered by exact match first, then tx count)
        Assert.Equal("Cardano Local", results[0].Name); // Exact match, 2 txs
        Assert.Equal("Adax Local", results[1].Name); // Non-exact, 1 tx

        // Tier 2: Exact matches without transactions (ordered by MarketCapRank, local null rank is last among Tier 2, external has rank 5)
        Assert.Equal("Cardano External", results[2].Name); // External, rank 5
        Assert.Equal("Cardano NoTx", results[3].Name); // Local, no rank

        // Tier 3: Remaining local (alphabetical)
        Assert.Equal("AdaB Local", results[4].Name);

        // Tier 4: Remaining external (ordered by rank)
        Assert.Equal("AdaC External", results[5].Name); // Rank 1
        Assert.Equal("AdaD External", results[6].Name); // Rank 10
    }
}
