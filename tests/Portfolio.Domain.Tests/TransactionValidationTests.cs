using System;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Xunit;

namespace Portfolio.Domain.Tests;

public class TransactionValidationTests
{
    private readonly Guid _assetId = Guid.NewGuid();

    [Fact]
    public void Constructor_Reward_ShouldSucceed_WithToAssetOnly()
    {
        // Arrange & Act
        var tx = new Transaction(
            DateTime.UtcNow,
            TransactionType.Reward,
            null, // FromAssetId
            _assetId, // ToAssetId
            0, 1, 2000, null, 0, null, null, null, null, FiatCurrency.USD, null, null
        );

        // Assert
        Assert.Equal(TransactionType.Reward, tx.Type);
        Assert.Null(tx.FromAssetId);
        Assert.Equal(_assetId, tx.ToAssetId);
    }

    [Fact]
    public void Constructor_Withdrawal_ShouldSucceed_WithFromAssetOnly()
    {
        // Arrange & Act
        var fiatId = FiatCurrency.TaxCurrency.Id;
        var tx = new Transaction(
            DateTime.UtcNow,
            TransactionType.Withdrawal,
            fromAssetId: fiatId,
            toAssetId: null,
            1, 0, 50000, null, 0, null, null, null, null, FiatCurrency.USD, null, null
        );

        // Assert
        Assert.Equal(TransactionType.Withdrawal, tx.Type);
        Assert.Equal(fiatId, tx.FromAssetId);
        Assert.Null(tx.ToAssetId);
    }

    [Fact]
    public void Constructor_Deposit_ShouldSucceed_WithToAssetOnly()
    {
        // Arrange & Act
        var fiatId = FiatCurrency.TaxCurrency.Id;
        var tx = new Transaction(
            DateTime.UtcNow,
            TransactionType.Deposit,
            fromAssetId: null,
            toAssetId: fiatId,
            0, 100, 1, null, 0, null, null, null, null, FiatCurrency.USD, null, null
        );

        // Assert
        Assert.Equal(TransactionType.Deposit, tx.Type);
        Assert.Null(tx.FromAssetId);
        Assert.Equal(fiatId, tx.ToAssetId);
    }

    [Fact]
    public void Constructor_Swap_ShouldSucceed_WithBothAssets()
    {
        // Arrange & Act
        var tx = new Transaction(
            DateTime.UtcNow,
            TransactionType.Swap,
            _assetId, // FromAssetId
            Guid.NewGuid(), // ToAssetId
            100, 1, 1, null, 0, null, null, null, null, FiatCurrency.USD, null, null
        );

        // Assert
        Assert.Equal(TransactionType.Swap, tx.Type);
        Assert.NotNull(tx.FromAssetId);
        Assert.NotNull(tx.ToAssetId);
    }

    [Fact]
    public void Constructor_Swap_ShouldFail_WhenFromAssetIsNull()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new Transaction(
            DateTime.UtcNow,
            TransactionType.Swap,
            null, // FromAssetId null
            _assetId,
            100, 1, 1, null, 0, null, null, null, null, FiatCurrency.USD, null, null
        ));
        Assert.Contains("FromAssetId is required", ex.Message);
    }

    [Fact]
    public void Constructor_Reward_ShouldFail_WhenFromAssetIsProvided()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new Transaction(
            DateTime.UtcNow,
            TransactionType.Reward,
            _assetId, // FromAssetId provided
            _assetId,
            0, 1, 1, null, 0, null, null, null, null, FiatCurrency.USD, null, null
        ));
        Assert.Contains("FromAssetId must be null", ex.Message);
    }

    [Fact]
    public void Constructor_Withdrawal_ShouldFail_WhenToAssetIsProvided()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new Transaction(
            DateTime.UtcNow,
            TransactionType.Withdrawal,
            _assetId,
            _assetId, // ToAssetId provided
            1, 0, 1, null, 0, null, null, null, null, FiatCurrency.USD, null, null
        ));
        Assert.Contains("ToAssetId must be null", ex.Message);
    }

    [Fact]
    public void Constructor_ShouldFail_WhenBothPricesAreNull()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new Transaction(
            DateTime.UtcNow,
            TransactionType.Swap,
            _assetId,
            Guid.NewGuid(),
            100, 1,
            null, null, // Both Prices Null
            0, null, null, null, null, FiatCurrency.USD, null, null
        ));
        Assert.Contains("Either SpotPriceUSD or SpotPriceEUR must logically have a value depending on the Input Currency.", ex.Message);
    }

    [Fact]
    public void Constructor_Loss_ShouldSucceed_WithFromAssetOnly()
    {
        // Arrange & Act
        var tx = new Transaction(
            DateTime.UtcNow,
            TransactionType.Loss,
            _assetId, // FromAssetId
            null, // ToAssetId
            1, 0, 1, null, 0, null, null, null, null, FiatCurrency.USD, null, null
        );

        // Assert
        Assert.Equal(TransactionType.Loss, tx.Type);
        Assert.Equal(_assetId, tx.FromAssetId);
        Assert.Null(tx.ToAssetId);
    }

    [Fact]
    public void Constructor_Loss_ShouldFail_WhenToAssetIsProvided()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new Transaction(
            DateTime.UtcNow,
            TransactionType.Loss,
            _assetId,
            _assetId, // ToAssetId provided
            1, 0, 1, null, 0, null, null, null, null, FiatCurrency.USD, null, null
        ));
        Assert.Contains("ToAssetId must be null", ex.Message);
    }

    [Fact]
    public void Constructor_ShouldFail_WhenDateIsInFuture()
    {
        // Act & Assert
        var futureDate = DateTime.UtcNow.AddDays(1);
        var ex = Assert.Throws<ArgumentException>(() => new Transaction(
            futureDate,
            TransactionType.Deposit,
            null,
            FiatCurrency.TaxCurrency.Id,
            0, 100, 1, null, 0, null, null, null, null, FiatCurrency.USD, null, null
        ));
        Assert.Contains("Transaction date cannot be in the future", ex.Message);
    }

    [Fact]
    public void Update_ShouldFail_WhenDateIsInFuture()
    {
        // Arrange
        var tx = new Transaction(
            DateTime.UtcNow,
            TransactionType.Deposit,
            null,
            FiatCurrency.TaxCurrency.Id,
            0, 100, 1, null, 0, null, null, null, null, FiatCurrency.USD, null, null
        );

        // Act & Assert
        var futureDate = DateTime.UtcNow.AddDays(1);
        var ex = Assert.Throws<ArgumentException>(() => tx.Update(
            futureDate,
            TransactionType.Deposit,
            null,
            FiatCurrency.TaxCurrency.Id,
            0, 100, 1, null, 0, null, null, null, FiatCurrency.USD, null, null
        ));
        Assert.Contains("Transaction date cannot be in the future", ex.Message);
    }
}
