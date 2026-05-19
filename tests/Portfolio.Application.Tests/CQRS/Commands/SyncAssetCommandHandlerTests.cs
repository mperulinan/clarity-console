using NSubstitute;
using Portfolio.Application.CQRS.Commands;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using Portfolio.Application.Tests.Fakes;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Portfolio.Application.Tests.CQRS.Commands;

public class SyncAssetCommandHandlerTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_InvalidExternalId_ThrowsArgumentException(string? externalId)
    {
        // Arrange
        var uow = new FakeUnitOfWork();
        var syncService = Substitute.For<IAssetSynchronizationService>();
        var logoProviders = new List<IAssetLogoProvider>();
        var handler = new SyncAssetCommandHandler(uow, syncService, logoProviders, NullLogger<SyncAssetCommandHandler>.Instance);

        var request = new AssetDto
        {
            ExternalId = externalId,
            Symbol = "BTC",
            Name = "Bitcoin",
            Type = "CRYPTO"
        };
        var command = new SyncAssetCommand(request);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ExistingAssetWithLogo_ReturnsExistingAssetWithoutCallingSyncServiceOrProviders()
    {
        // Arrange
        var uow = new FakeUnitOfWork();
        var existingAsset = new Asset("BTC", "Bitcoin", "btc-ext", "http://example.com/btc.png", AssetType.Crypto);
        await uow.Assets.AddAsync(existingAsset);

        var syncService = Substitute.For<IAssetSynchronizationService>();
        var logoProvider = Substitute.For<IAssetLogoProvider>();
        var logoProviders = new List<IAssetLogoProvider> { logoProvider };

        var handler = new SyncAssetCommandHandler(uow, syncService, logoProviders, NullLogger<SyncAssetCommandHandler>.Instance);

        var request = new AssetDto
        {
            ExternalId = "btc-ext",
            Symbol = "BTC",
            Name = "Bitcoin",
            ImageUrl = "http://example.com/btc.png",
            Type = "CRYPTO"
        };
        var command = new SyncAssetCommand(request);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingAsset.Id, result.Id);
        Assert.Equal("http://example.com/btc.png", result.ImageUrl);
        
        await syncService.DidNotReceive().SynchronizeCatalogAsync(Arg.Any<IEnumerable<Asset>>());
        await logoProvider.DidNotReceive().GetLogoUrlAsync(Arg.Any<string>());
        Assert.Equal(0, uow.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_ExistingAssetWithoutLogo_CallsSupportedLogoProviderAndUpdatesAsset()
    {
        // Arrange
        var uow = new FakeUnitOfWork();
        var existingAsset = new Asset("AAPL", "Apple Inc.", "aapl-ext", null, AssetType.Stock);
        await uow.Assets.AddAsync(existingAsset);

        var syncService = Substitute.For<IAssetSynchronizationService>();
        var logoProvider = Substitute.For<IAssetLogoProvider>();
        logoProvider.Supports(AssetType.Stock).Returns(true);
        logoProvider.GetLogoUrlAsync("AAPL").Returns(Task.FromResult<string?>("http://example.com/aapl.png"));

        var logoProviders = new List<IAssetLogoProvider> { logoProvider };
        var handler = new SyncAssetCommandHandler(uow, syncService, logoProviders, NullLogger<SyncAssetCommandHandler>.Instance);

        var request = new AssetDto
        {
            ExternalId = "aapl-ext",
            Symbol = "AAPL",
            Name = "Apple Inc.",
            Type = "STOCK"
        };
        var command = new SyncAssetCommand(request);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingAsset.Id, result.Id);
        Assert.Equal("http://example.com/aapl.png", result.ImageUrl);

        // Verify logo provider was queried
        logoProvider.Received(1).Supports(AssetType.Stock);
        await logoProvider.Received(1).GetLogoUrlAsync("AAPL");

        // Verify asset updated in repo and saved
        var savedAsset = (await uow.Assets.GetByExternalIdsAsync(["aapl-ext"])).Single();
        Assert.Equal("http://example.com/aapl.png", savedAsset.ImageUrl);
        Assert.Equal(1, uow.SaveChangesCallCount);
        
        await syncService.DidNotReceive().SynchronizeCatalogAsync(Arg.Any<IEnumerable<Asset>>());
    }

    [Fact]
    public async Task Handle_ExistingAssetWithoutLogo_NoSupportedProvider_KeepsLogoNullAndUpdatesAsset()
    {
        // Arrange
        var uow = new FakeUnitOfWork();
        var existingAsset = new Asset("USD", "US Dollar", "usd-ext", null, AssetType.Fiat);
        await uow.Assets.AddAsync(existingAsset);

        var syncService = Substitute.For<IAssetSynchronizationService>();
        var logoProvider = Substitute.For<IAssetLogoProvider>();
        logoProvider.Supports(Arg.Any<AssetType>()).Returns(false);

        var logoProviders = new List<IAssetLogoProvider> { logoProvider };
        var handler = new SyncAssetCommandHandler(uow, syncService, logoProviders, NullLogger<SyncAssetCommandHandler>.Instance);

        var request = new AssetDto
        {
            ExternalId = "usd-ext",
            Symbol = "USD",
            Name = "US Dollar",
            Type = "FIAT"
        };
        var command = new SyncAssetCommand(request);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingAsset.Id, result.Id);
        Assert.Null(result.ImageUrl);

        logoProvider.Received(1).Supports(AssetType.Fiat);
        await logoProvider.DidNotReceive().GetLogoUrlAsync(Arg.Any<string>());

        var savedAsset = (await uow.Assets.GetByExternalIdsAsync(["usd-ext"])).Single();
        Assert.Null(savedAsset.ImageUrl);
        Assert.Equal(1, uow.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_NewAsset_CallsSupportedLogoProviderAndSynchronizesCatalog()
    {
        // Arrange
        var uow = new FakeUnitOfWork();
        var syncService = Substitute.For<IAssetSynchronizationService>();
        
        // Setup synchronization service to actually register the asset in the fake repository
        syncService.When(s => s.SynchronizeCatalogAsync(Arg.Any<IEnumerable<Asset>>()))
            .Do(async call =>
            {
                var assets = call.Arg<IEnumerable<Asset>>();
                foreach (var asset in assets)
                {
                    await uow.Assets.AddAsync(asset);
                }
            });

        var logoProvider = Substitute.For<IAssetLogoProvider>();
        logoProvider.Supports(AssetType.Stock).Returns(true);
        logoProvider.GetLogoUrlAsync("AAPL").Returns(Task.FromResult<string?>("http://example.com/aapl.png"));

        var logoProviders = new List<IAssetLogoProvider> { logoProvider };
        var handler = new SyncAssetCommandHandler(uow, syncService, logoProviders, NullLogger<SyncAssetCommandHandler>.Instance);

        var request = new AssetDto
        {
            ExternalId = "aapl-ext",
            Symbol = "AAPL",
            Name = "Apple Inc.",
            Type = "STOCK"
        };
        var command = new SyncAssetCommand(request);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("aapl-ext", result.ExternalId);
        Assert.Equal("http://example.com/aapl.png", result.ImageUrl);

        await logoProvider.Received(1).GetLogoUrlAsync("AAPL");
        await syncService.Received(1).SynchronizeCatalogAsync(Arg.Is<IEnumerable<Asset>>(assets => 
            assets.Single().ExternalId == "aapl-ext" && assets.Single().ImageUrl == "http://example.com/aapl.png"));

        var savedAsset = (await uow.Assets.GetByExternalIdsAsync(["aapl-ext"])).Single();
        Assert.Equal("http://example.com/aapl.png", savedAsset.ImageUrl);
    }

    [Fact]
    public async Task Handle_NewAsset_SyncFailsToRegisterAsset_ThrowsInvalidOperationException()
    {
        // Arrange
        var uow = new FakeUnitOfWork();
        var syncService = Substitute.For<IAssetSynchronizationService>();
        // syncService does NOT register the asset in uow.Assets

        var logoProvider = Substitute.For<IAssetLogoProvider>();
        var logoProviders = new List<IAssetLogoProvider> { logoProvider };
        var handler = new SyncAssetCommandHandler(uow, syncService, logoProviders, NullLogger<SyncAssetCommandHandler>.Instance);

        var request = new AssetDto
        {
            ExternalId = "aapl-ext",
            Symbol = "AAPL",
            Name = "Apple Inc.",
            Type = "STOCK"
        };
        var command = new SyncAssetCommand(request);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => handler.Handle(command, CancellationToken.None));
    }
}
