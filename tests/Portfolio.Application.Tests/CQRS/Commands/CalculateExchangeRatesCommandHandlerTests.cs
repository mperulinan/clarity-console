using NSubstitute;
using Portfolio.Application.CQRS.Commands;
using Portfolio.Application.Tests.Fakes;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace Portfolio.Application.Tests.CQRS.Commands;

public class CalculateExchangeRatesCommandHandlerTests
{
    private readonly IExchangeRateProvider _mockRates;

    public CalculateExchangeRatesCommandHandlerTests()
    {
        _mockRates = Substitute.For<IExchangeRateProvider>();
    }

    private static Transaction CreateTx(
        DateTime? date = null,
        TransactionType? type = null,
        string? fromAsset = null,
        string? toAsset = null,
        decimal spent = 0,
        decimal received = 0,
        decimal spotPriceUSD = 1,
        decimal? spotPriceEUR = null,
        decimal fee = 0,
        string? feeAsset = null,
        decimal? feeUsdPrice = null,
        decimal? feeEurPrice = null,
        decimal? xr = null,
        FiatCurrency? spotCurrency = null,
        FiatCurrency? feeCurrency = null,
        string? notes = null)
    {
        return new Transaction(
            date ?? DateTime.UtcNow,
            type ?? TransactionType.Swap,
            Guid.NewGuid(),
            Guid.NewGuid(),
            spent,
            received,
            spotPriceUSD,
            spotPriceEUR,
            fee,
            feeAsset != null ? Guid.NewGuid() : null,
            feeUsdPrice,
            feeEurPrice,
            xr,
            spotCurrency ?? (spotPriceEUR.HasValue && spotPriceUSD == 0 ? FiatCurrency.EUR : FiatCurrency.USD),
            feeCurrency,
            notes)
        {
            FromAsset = new Asset(fromAsset ?? "USD", fromAsset ?? "USD", null, null, AssetType.Fiat),
            ToAsset = new Asset(toAsset ?? "BTC", toAsset ?? "BTC", null, null, AssetType.Fiat)
        };
    }

    [Fact]
    public async Task Handle_ShouldUpdateRates_ForPastTransactions()
    {
        var uow = new FakeUnitOfWork();
        var pastDate = DateTime.UtcNow.AddDays(-2);
        var tx = CreateTx(pastDate, TransactionType.Swap, "USD", "BTC", 100, 1, 1);
        await uow.FakeTransactions.AddAsync(tx);

        _mockRates.GetUsdEurRateAsync(Arg.Any<DateTime>()).Returns(Task.FromResult(0.85m));

        var handler = new CalculateExchangeRatesCommandHandler(uow, _mockRates, NullLogger<CalculateExchangeRatesCommandHandler>.Instance);
        await handler.Handle(new CalculateExchangeRatesCommand(), CancellationToken.None);

        Assert.Equal(0.85m, tx.UsdEurExchangeRate);
        Assert.Equal(0.85m, tx.SpotPriceEUR);
        Assert.Equal(1, uow.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_ShouldBatchSave_WhenMultiplePendingTransactions()
    {
        var uow = new FakeUnitOfWork();
        var pastDate = DateTime.UtcNow.AddDays(-2);

        for (int i = 0; i < 3; i++)
            await uow.FakeTransactions.AddAsync(CreateTx(pastDate, TransactionType.Swap, "USD", "BTC", 100, 1, 1));

        _mockRates.GetUsdEurRateAsync(Arg.Any<DateTime>()).Returns(Task.FromResult(0.9m));

        var handler = new CalculateExchangeRatesCommandHandler(uow, _mockRates, NullLogger<CalculateExchangeRatesCommandHandler>.Instance);
        await handler.Handle(new CalculateExchangeRatesCommand(), CancellationToken.None);

        Assert.Equal(1, uow.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_ShouldUpdateUsdFromEur_WhenEurIsTruthSource()
    {
        var uow = new FakeUnitOfWork();
        var pastDate = DateTime.UtcNow.AddDays(-3);
        var tx = new Transaction(pastDate, TransactionType.Swap, Guid.NewGuid(), Guid.NewGuid(), 1, 1, null, 100m, 0, null, null, null, null, FiatCurrency.EUR, null, null)
        {
            FromAsset = new Asset("EUR", "Euro", null, null, AssetType.Fiat),
            ToAsset = new Asset("BTC", "Bitcoin", null, null, AssetType.Crypto)
        };
        await uow.FakeTransactions.AddAsync(tx);

        _mockRates.GetUsdEurRateAsync(Arg.Any<DateTime>()).Returns(Task.FromResult(0.8m));

        var handler = new CalculateExchangeRatesCommandHandler(uow, _mockRates, NullLogger<CalculateExchangeRatesCommandHandler>.Instance);
        await handler.Handle(new CalculateExchangeRatesCommand(), CancellationToken.None);

        Assert.Equal(125m, tx.SpotPriceUSD);
        Assert.Equal(100m, tx.SpotPriceEUR);
    }
}
