using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.ValueObjects;
using Xunit;

namespace Portfolio.Domain.Tests;

public class TransactionTests
{
    [Fact]
    public void GetToAssetPrice_ShouldCalculateBasedOnSpentAndReceived()
    {
        // Spent 100 USD (Price 1) to get 2 BTC.
        // ToAssetPrice = 100 * 1 / 2 = 50.
        Transaction tx = new(DateTime.Now, TransactionType.Swap, "USD", "BTC", 100m, 2m, 1m, null, 0, null, null, null, null, null);

        var price = tx.GetToAssetPrice(FiatCurrency.USD);

        Assert.Equal(50m, price);
    }

    [Fact]
    public void UpdateExchangeRates_ShouldUpdateEurPrices()
    {
        // USD Price = 100. No EUR Price.
        // Exchange Rate = 0.9.
        // Expected EUR Price = 90.
        var tx = new Transaction(DateTime.Now, TransactionType.Swap, "USD", "BTC", 100m, 2m, 100m, null, 0, null, 10m, null, null, null); // feeAssetPriceInUsd = 10

        tx.UpdateExchangeRates(0.9m);

        Assert.Equal(0.9m, tx.UsdEurExchangeRate);
        Assert.Equal(90m, tx.FromAssetPriceInEur);
        Assert.Equal(9m, tx.FeeAssetPriceInEur); // 10 * 0.9
    }
}
