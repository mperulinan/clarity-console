using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;
using Portfolio.Infrastructure.ExternalServices;

namespace Portfolio.Infrastructure.Tests;

public class AssetSearchCacheServiceTests
{
    private readonly IAssetSearchProvider _innerProvider;
    private readonly IMemoryCache _cache;
    private readonly AssetSearchCacheService _sut;

    public AssetSearchCacheServiceTests()
    {
        _innerProvider = Substitute.For<IAssetSearchProvider>();
        
        var options = Substitute.For<IOptions<MemoryCacheOptions>>();
        options.Value.Returns(new MemoryCacheOptions());
        _cache = new MemoryCache(options);

        _sut = new AssetSearchCacheService(
            _innerProvider,
            _cache,
            new NullLogger<AssetSearchCacheService>()
        );
    }

    [Fact]
    public async Task SearchAssetsAsync_EmptyQuery_ReturnsEmptyList()
    {
        // Act
        var results = await _sut.SearchAssetsAsync("");

        // Assert
        Assert.Empty(results);
        await _innerProvider.DidNotReceive().SearchAssetsAsync(Arg.Any<string>());
    }

    [Fact]
    public async Task SearchAssetsAsync_WhenCalled_CachesResults()
    {
        // Arrange
        var query = "BTC";
        var asset = new Asset("BTC", "Bitcoin", null, null, AssetType.Crypto);
        var searchResult = new SearchAssetResult(asset, 1);
        
        _innerProvider.SearchAssetsAsync(query).Returns([searchResult]);

        // Act - First call
        var result1 = await _sut.SearchAssetsAsync(query);

        // Act - Second call
        var result2 = await _sut.SearchAssetsAsync(query);

        // Assert
        Assert.Single(result1);
        Assert.Single(result2);

        // Verify that the inner provider was only called once
        await _innerProvider.Received(1).SearchAssetsAsync(query);
    }

    [Fact]
    public async Task SearchAssetsAsync_DifferentQuery_DoesNotUseCache()
    {
        // Arrange
        var asset1 = new Asset("BTC", "Bitcoin", null, null, AssetType.Crypto);
        var searchResult1 = new SearchAssetResult(asset1, 1);
        
        var asset2 = new Asset("ETH", "Ethereum", null, null, AssetType.Crypto);
        var searchResult2 = new SearchAssetResult(asset2, 2);

        _innerProvider.SearchAssetsAsync("BTC").Returns([searchResult1]);
        _innerProvider.SearchAssetsAsync("ETH").Returns([searchResult2]);

        // Act - First query
        await _sut.SearchAssetsAsync("BTC");

        // Act - Different query
        await _sut.SearchAssetsAsync("ETH");

        // Assert
        // Inner provider should be hit twice since the second call is a different query
        await _innerProvider.Received(1).SearchAssetsAsync("BTC");
        await _innerProvider.Received(1).SearchAssetsAsync("ETH");
    }
}
