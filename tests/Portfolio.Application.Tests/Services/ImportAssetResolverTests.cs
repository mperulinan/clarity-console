using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Portfolio.Application.CQRS.Commands;
using Portfolio.Application.DTOs;
using Portfolio.Application.Services;
using Portfolio.Application.Tests.Fakes;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;

namespace Portfolio.Application.Tests.Services;

public class ImportAssetResolverTests
{
    private readonly FakeUnitOfWork _uow;
    private readonly IMediator _mediator;
    private readonly ImportAssetResolver _sut;

    public ImportAssetResolverTests()
    {
        _uow = new FakeUnitOfWork();
        _mediator = Substitute.For<IMediator>();
        _sut = new ImportAssetResolver(_uow, _mediator, NullLogger<ImportAssetResolver>.Instance);
    }

    [Fact]
    public async Task ResolveMissingAssetsAsync_ExistingAsset_ReturnsIdWithoutSendingSyncCommand()
    {
        // Arrange
        var existingAsset = new Asset("AAPL", "Apple Inc", "AAPL", null, AssetType.Stock);
        await _uow.Assets.AddAsync(existingAsset);

        // Act
        var result = await _sut.ResolveMissingAssetsAsync(["AAPL"]);

        // Assert
        Assert.Single(result);
        Assert.Equal(existingAsset.Id, result["AAPL"]);
        await _mediator.DidNotReceive().Send(Arg.Any<SyncAssetCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveMissingAssetsAsync_NonExistingAsset_SendsSyncCommandAndResolvesId()
    {
        // Arrange
        var newlyCreatedAsset = new Asset("MSFT", "Microsoft Corp", "MSFT", null, AssetType.Stock);
        _mediator.Send(Arg.Any<SyncAssetCommand>(), Arg.Any<CancellationToken>())
            .Returns(async _ =>
            {
                await _uow.Assets.AddAsync(newlyCreatedAsset);
                return new AssetDto { Id = newlyCreatedAsset.Id, Symbol = "MSFT" };
            });

        // Act
        var result = await _sut.ResolveMissingAssetsAsync(["MSFT"]);

        // Assert
        Assert.Single(result);
        Assert.Equal(newlyCreatedAsset.Id, result["MSFT"]);
        await _mediator.Received(1).Send(Arg.Is<SyncAssetCommand>(cmd => cmd.Asset.Symbol == "MSFT"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResolveMissingAssetsAsync_EmptyOrWhitespaceSymbols_ReturnsEmptyDictionary()
    {
        // Act
        var result = await _sut.ResolveMissingAssetsAsync(["", "  "]);

        // Assert
        Assert.Empty(result);
        await _mediator.DidNotReceive().Send(Arg.Any<SyncAssetCommand>(), Arg.Any<CancellationToken>());
    }
}

