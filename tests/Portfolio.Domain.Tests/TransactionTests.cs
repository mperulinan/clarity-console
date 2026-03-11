using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.ValueObjects;
using Xunit;

namespace Portfolio.Domain.Tests;

public class TransactionTests
{
    private static Transaction CreateTx(DateTime date, TransactionType type, string fromAsset, string toAsset, decimal spent, decimal received, decimal fromAssetPriceUsd, decimal? fromAssetPriceEur, decimal fee, string? feeAsset, decimal? feeUsdPrice, decimal? feeEurPrice, decimal? xr, string? notes)
    {
        var fromAsstId = Guid.NewGuid();
        var toAsstId = fromAsset == toAsset ? fromAsstId : Guid.NewGuid();
        var feeAsstId = feeAsset != null ? (feeAsset == fromAsset ? fromAsstId : (feeAsset == toAsset ? toAsstId : Guid.NewGuid())) : (Guid?)null;

        var tx = new Transaction(date, type, fromAsstId, toAsstId, spent, received, fromAssetPriceUsd, fromAssetPriceEur, fee, feeAsstId, feeUsdPrice, feeEurPrice, xr, notes)
        {
            FromAsset = new Asset(fromAsset, fromAsset, null, null, AssetType.Crypto),
            ToAsset = new Asset(toAsset, toAsset, null, null, AssetType.Crypto)
        };
        if (feeAsset != null) tx.FeeAsset = new Asset(feeAsset, feeAsset, null, null, AssetType.Crypto);
        return tx;
    }
    [Fact]
    public void GetToAssetPrice_ShouldCalculateBasedOnSpentAndReceived()
    {
        // Spent 100 USD (Price 1) to get 2 BTC.
        // ToAssetPrice = 100 * 1 / 2 = 50.
        Transaction tx = CreateTx(DateTime.Now, TransactionType.Swap, "USD", "BTC", 100m, 2m, 1m, null, 0, null, null, null, null, null);

        var price = tx.GetToAssetPrice(FiatCurrency.USD);

        Assert.Equal(50m, price);
    }

    [Fact]
    public void UpdateExchangeRates_ShouldUpdateEurPrices()
    {
        // USD Price = 100. No EUR Price.
        // Exchange Rate = 0.9.
        // Expected EUR Price = 90.
        var tx = CreateTx(DateTime.Now, TransactionType.Swap, "USD", "BTC", 100m, 2m, 100m, null, 0, null, 10m, null, null, null); // feeAssetPriceInUsd = 10

        tx.UpdateExchangeRates(0.9m);

        Assert.Equal(0.9m, tx.UsdEurExchangeRate);
        Assert.Equal(90m, tx.SpotPriceInEur);
        Assert.Equal(9m, tx.FeeSpotPriceInEur); // 10 * 0.9
    }
}
