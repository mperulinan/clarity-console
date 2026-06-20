using NSubstitute;
using Portfolio.Application.CQRS.Queries;
using Portfolio.Application.Tests.Fakes;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;
using Xunit;

namespace Portfolio.Application.Tests.CQRS.Queries;

public class GetAssetSpotPriceQueryHandlerTests
{
    private readonly FakeUnitOfWork _uow;
    private readonly IAssetPriceProviderFactory _mockPriceProviderFactory;
    private readonly IAssetPriceProvider _mockPriceProvider;

    public GetAssetSpotPriceQueryHandlerTests()
    {
        _uow = new FakeUnitOfWork();
        _mockPriceProviderFactory = Substitute.For<IAssetPriceProviderFactory>();
        _mockPriceProvider = Substitute.For<IAssetPriceProvider>();
    }

    [Fact]
    public async Task Handle_WithValidRequest_SpotPrice_ShouldReturnPrice()
    {
        // Arrange
        var asset = new Asset("BTC", "Bitcoin", "bitcoin", null, AssetType.Crypto);
        await _uow.Assets.AddAsync(asset);

        _mockPriceProviderFactory.GetProvider(AssetType.Crypto).Returns(_mockPriceProvider);
        _mockPriceProvider.GetPricesAsync(Arg.Is<IEnumerable<string>>(x => x.Contains("bitcoin")), FiatCurrency.USD)
            .Returns(Task.FromResult(new Dictionary<string, decimal> { { "bitcoin", 50000m } }));

        var handler = new GetAssetSpotPriceQueryHandler(_uow, _mockPriceProviderFactory);
        var query = new GetAssetSpotPriceQuery(asset.Id, "usd", null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(50000m, result);
    }

    [Fact]
    public async Task Handle_WithHistoricalDate_ShouldReturnHistoricalPrice()
    {
        // Arrange
        var asset = new Asset("BTC", "Bitcoin", "bitcoin", null, AssetType.Crypto);
        await _uow.Assets.AddAsync(asset);

        _mockPriceProviderFactory.GetProvider(AssetType.Crypto).Returns(_mockPriceProvider);
        
        var historicalDate = DateTime.UtcNow.AddDays(-5); // Past date
        _mockPriceProvider.GetHistoricalPriceAsync("bitcoin", FiatCurrency.EUR, historicalDate)
            .Returns(Task.FromResult((decimal?)45000m));

        var handler = new GetAssetSpotPriceQueryHandler(_uow, _mockPriceProviderFactory);
        var query = new GetAssetSpotPriceQuery(asset.Id, "eur", historicalDate);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(45000m, result);
    }

    [Fact]
    public async Task Handle_InvalidFiatCurrency_ShouldThrowArgumentException()
    {
        // Arrange
        var handler = new GetAssetSpotPriceQueryHandler(_uow, _mockPriceProviderFactory);
        var query = new GetAssetSpotPriceQuery(Guid.NewGuid(), "invalid", null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AssetNotFound_ShouldReturnNull()
    {
        // Arrange
        var handler = new GetAssetSpotPriceQueryHandler(_uow, _mockPriceProviderFactory);
        var query = new GetAssetSpotPriceQuery(Guid.NewGuid(), "usd", null); // Random ID

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_AssetMissingExternalId_ShouldReturnNull()
    {
        // Arrange
        var asset = new Asset("BTC", "Bitcoin", null, null, AssetType.Crypto); // Null ExternalId
        await _uow.Assets.AddAsync(asset);

        var handler = new GetAssetSpotPriceQueryHandler(_uow, _mockPriceProviderFactory);
        var query = new GetAssetSpotPriceQuery(asset.Id, "usd", null);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_NoPriceProviderAvailable_ShouldThrowArgumentException()
    {
        // Arrange
        var asset = new Asset("CASH", "Cash", "cash-id", null, AssetType.Fiat);
        await _uow.Assets.AddAsync(asset);

        // Mock returns null for Fiat provider
        _mockPriceProviderFactory.GetProvider(AssetType.Fiat).Returns((IAssetPriceProvider?)null);

        var handler = new GetAssetSpotPriceQueryHandler(_uow, _mockPriceProviderFactory);
        var query = new GetAssetSpotPriceQuery(asset.Id, "usd", null);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(query, CancellationToken.None));
    }
}
