using NSubstitute;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;
using Portfolio.Domain.Services;

namespace Portfolio.Domain.Tests;

public class AssetMarketDataServiceTests
{
    private readonly IAssetRepository _assetRepository;
    private readonly IAssetPriceProviderFactory _priceProviderFactory;
    private readonly IExchangeRateProvider _exchangeRateProvider;
    private readonly AssetMarketDataService _sut;

    public AssetMarketDataServiceTests()
    {
        _assetRepository = Substitute.For<IAssetRepository>();
        _priceProviderFactory = Substitute.For<IAssetPriceProviderFactory>();
        _exchangeRateProvider = Substitute.For<IExchangeRateProvider>();

        _sut = new AssetMarketDataService(
            _assetRepository,
            _priceProviderFactory,
            _exchangeRateProvider
        );
    }

    [Fact]
    public async Task GetMarketDataAsync_OtherAssetType_ReturnsZeroPrice()
    {
        // Arrange
        var asset = new Asset("COLLECTIBLE", "Rare Coin", null, null, AssetType.Other);
        _assetRepository.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>())
            .Returns([asset]);

        // Act
        var result = await _sut.GetMarketDataAsync([asset.Id], FiatCurrency.USD);

        // Assert
        Assert.True(result.ContainsKey(asset.Id));
        Assert.Equal(0m, result[asset.Id].Price);
        Assert.Equal("COLLECTIBLE", result[asset.Id].Symbol);
        // Should not call the price provider factory for "Other" types
        _priceProviderFactory.DidNotReceive().GetProvider(Arg.Any<AssetType>());
    }

    [Fact]
    public async Task GetMarketDataAsync_FiatAsset_SameCurrencyAsBase_ReturnsOnePoint()
    {
        // Arrange
        var usd = new Asset("USD", "US Dollar", null, null, AssetType.Fiat);
        _assetRepository.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>())
            .Returns([usd]);

        // Act
        var result = await _sut.GetMarketDataAsync([usd.Id], FiatCurrency.USD);

        // Assert
        Assert.Equal(1.0m, result[usd.Id].Price);
        // Exchange rate provider should never be called for same currency
        await _exchangeRateProvider.DidNotReceive().GetExchangeRateAsync(Arg.Any<FiatCurrency>(), Arg.Any<FiatCurrency>());
    }

    [Fact]
    public async Task GetMarketDataAsync_FiatAsset_DifferentCurrency_CallsExchangeRateProvider()
    {
        // Arrange
        var eur = new Asset("EUR", "Euro", null, null, AssetType.Fiat);
        _assetRepository.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>())
            .Returns([eur]);
        _exchangeRateProvider
            .GetExchangeRateAsync(FiatCurrency.EUR, FiatCurrency.USD)
            .Returns(1.08m);

        // Act
        var result = await _sut.GetMarketDataAsync([eur.Id], FiatCurrency.USD);

        // Assert
        Assert.Equal(1.08m, result[eur.Id].Price);
        await _exchangeRateProvider.Received(1)
            .GetExchangeRateAsync(FiatCurrency.EUR, FiatCurrency.USD);
    }

    [Fact]
    public async Task GetMarketDataAsync_CryptoAsset_NoProviderRegistered_ReturnsZeroPrice()
    {
        // Arrange
        var btc = new Asset("BTC", "Bitcoin", "bitcoin", null, AssetType.Crypto);
        _assetRepository.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>())
            .Returns([btc]);
        _priceProviderFactory.GetProvider(AssetType.Crypto).Returns((IAssetPriceProvider?)null);

        // Act
        var result = await _sut.GetMarketDataAsync([btc.Id], FiatCurrency.USD);

        // Assert
        Assert.Equal(0m, result[btc.Id].Price);
        _priceProviderFactory.Received(1).GetProvider(AssetType.Crypto);
    }

    [Fact]
    public async Task GetMarketDataAsync_CryptoAsset_WithProvider_ReturnsMappedPrice()
    {
        // Arrange
        var btc = new Asset("BTC", "Bitcoin", "bitcoin", null, AssetType.Crypto);
        var eth = new Asset("ETH", "Ethereum", "ethereum", null, AssetType.Crypto);
        _assetRepository.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>())
            .Returns([btc, eth]);

        var mockProvider = Substitute.For<IAssetPriceProvider>();
        mockProvider.GetPricesAsync(Arg.Any<IEnumerable<string>>(), FiatCurrency.USD)
            .Returns(new Dictionary<string, decimal>
            {
                ["bitcoin"] = 65000m,
                ["ethereum"] = 3200m
            });
        _priceProviderFactory.GetProvider(AssetType.Crypto).Returns(mockProvider);

        // Act
        var result = await _sut.GetMarketDataAsync([btc.Id, eth.Id], FiatCurrency.USD);

        // Assert
        Assert.Equal(65000m, result[btc.Id].Price);
        Assert.Equal(3200m, result[eth.Id].Price);
        await mockProvider.Received(1).GetPricesAsync(
            Arg.Is<IEnumerable<string>>(ids => ids.Contains("bitcoin") && ids.Contains("ethereum")),
            FiatCurrency.USD);
    }

    [Fact]
    public async Task GetMarketDataAsync_CryptoAsset_MissingExternalId_ReturnsZeroPrice()
    {
        // Arrange — asset has no ExternalId so it won't be in the price map
        var unknown = new Asset("UNKNOWN", "Unknown Coin", null, null, AssetType.Crypto);
        _assetRepository.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>())
            .Returns([unknown]);

        var mockProvider = Substitute.For<IAssetPriceProvider>();
        mockProvider.GetPricesAsync(Arg.Any<IEnumerable<string>>(), FiatCurrency.USD)
            .Returns(new Dictionary<string, decimal>());
        _priceProviderFactory.GetProvider(AssetType.Crypto).Returns(mockProvider);

        // Act
        var result = await _sut.GetMarketDataAsync([unknown.Id], FiatCurrency.USD);

        // Assert
        Assert.Equal(0m, result[unknown.Id].Price);
    }
}
