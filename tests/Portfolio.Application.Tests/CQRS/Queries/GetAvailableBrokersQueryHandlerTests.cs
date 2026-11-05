using NSubstitute;
using Portfolio.Application.CQRS.Queries;
using Portfolio.Domain.Interfaces;

namespace Portfolio.Application.Tests.CQRS.Queries;

public class GetAvailableBrokersQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsAllRegisteredBrokersWithDisplayNames()
    {
        // Arrange
        var parser1 = Substitute.For<ITransactionImportParser>();
        parser1.BrokerKey.Returns("degiro");
        parser1.DisplayName.Returns("DEGIRO");

        var parser2 = Substitute.For<ITransactionImportParser>();
        parser2.BrokerKey.Returns("ibkr");
        parser2.DisplayName.Returns("Interactive Brokers");

        var handler = new GetAvailableBrokersQueryHandler([parser1, parser2]);

        // Act
        var result = await handler.Handle(new GetAvailableBrokersQuery(), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("degiro", result[0].Key);
        Assert.Equal("DEGIRO", result[0].DisplayName);
        Assert.Equal("ibkr", result[1].Key);
        Assert.Equal("Interactive Brokers", result[1].DisplayName);
    }

    [Fact]
    public async Task Handle_EmptyParsers_ReturnsEmptyList()
    {
        // Arrange
        var handler = new GetAvailableBrokersQueryHandler([]);

        // Act
        var result = await handler.Handle(new GetAvailableBrokersQuery(), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}

