using Portfolio.Domain.Enums;
using Portfolio.Domain.Services;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Domain.Entities;

public class Transaction
{
    public int Id { get; private set; }
    public DateTime Date { get; private set; }
    public TransactionType Type { get; private set; } = null!;
    public Guid? FromAssetId { get; private set; }
    public Asset? FromAsset { get; set; }
    public Guid? ToAssetId { get; private set; }
    public Asset? ToAsset { get; set; }
    public decimal AmountSpent { get; private set; }
    public decimal AmountReceived { get; private set; }

    /// <summary>
    /// Market unit price of the transaction's primary "priced asset" (FromAsset on disposals/swaps, ToAsset on acquisitions/deposits/rewards).
    /// </summary>
    public Price SpotPrice { get; private set; } = null!;

    public decimal Fee { get; private set; }
    public Guid? FeeAssetId { get; private set; }
    public Asset? FeeAsset { get; set; }

    /// <summary>
    /// Market unit price of the fee asset (<see cref="FeeAssetId"/>), if a fee was charged.
    /// </summary>
    public Price? FeePrice { get; private set; }

    /// <summary>
    /// Historical exchange rate between USD and EUR applicable on <see cref="Date"/> (1 USD = X EUR).
    /// </summary>
    public decimal? UsdEurExchangeRate { get; private set; }

    public string? Notes { get; private set; }
    public string? BrokerTransactionId { get; private set; }

    public bool HasIncompleteExchangeRates =>
        SpotPrice.IsIncomplete || (FeePrice != null && FeePrice.IsIncomplete);

    // Constructor for EF Core.
    private Transaction() { }

    public Transaction(
        DateTime date,
        TransactionType transactionType,
        Guid? fromAssetId,
        Guid? toAssetId,
        decimal amountSpent,
        decimal amountReceived,
        decimal? spotPriceUSD = null,
        decimal? spotPriceEUR = null,
        decimal fee = 0m,
        Guid? feeAssetId = null,
        decimal? feeSpotPriceUSD = null,
        decimal? feeSpotPriceEUR = null,
        decimal? usdEurExchangeRate = null,
        FiatCurrency? spotPriceInputCurrency = null,
        FiatCurrency? feePriceInputCurrency = null,
        string? notes = null,
        string? brokerTransactionId = null)
    {
        SetCoreDetails(
            date,
            transactionType,
            fromAssetId,
            toAssetId,
            amountSpent,
            amountReceived,
            spotPriceUSD,
            spotPriceEUR,
            fee,
            feeAssetId,
            feeSpotPriceUSD,
            feeSpotPriceEUR,
            spotPriceInputCurrency,
            feePriceInputCurrency,
            notes);

        UsdEurExchangeRate = usdEurExchangeRate;
        BrokerTransactionId = brokerTransactionId;
    }

    #region Factory Methods

    public static Transaction Create(
        DateTime date,
        TransactionType transactionType,
        Guid? fromAssetId,
        Guid? toAssetId,
        decimal amountSpent,
        decimal amountReceived,
        decimal? spotPriceUSD = null,
        decimal? spotPriceEUR = null,
        decimal fee = 0m,
        Guid? feeAssetId = null,
        decimal? feeSpotPriceUSD = null,
        decimal? feeSpotPriceEUR = null,
        decimal? usdEurExchangeRate = null,
        FiatCurrency? spotPriceInputCurrency = null,
        FiatCurrency? feePriceInputCurrency = null,
        string? notes = null,
        string? brokerTransactionId = null)
    {
        return new Transaction(
            date,
            transactionType,
            fromAssetId,
            toAssetId,
            amountSpent,
            amountReceived,
            spotPriceUSD,
            spotPriceEUR,
            fee,
            feeAssetId,
            feeSpotPriceUSD,
            feeSpotPriceEUR,
            usdEurExchangeRate,
            spotPriceInputCurrency,
            feePriceInputCurrency,
            notes,
            brokerTransactionId);
    }

    public static Transaction CreateSwap(
        DateTime date,
        Guid fromAssetId,
        Guid toAssetId,
        decimal amountSpent,
        decimal amountReceived,
        decimal? spotPriceUSD = null,
        decimal? spotPriceEUR = null,
        decimal fee = 0m,
        Guid? feeAssetId = null,
        decimal? feeSpotPriceUSD = null,
        decimal? feeSpotPriceEUR = null,
        decimal? usdEurExchangeRate = null,
        FiatCurrency? spotPriceInputCurrency = null,
        FiatCurrency? feePriceInputCurrency = null,
        string? notes = null,
        string? brokerTransactionId = null)
    {
        return Create(
            date,
            TransactionType.Swap,
            fromAssetId,
            toAssetId,
            amountSpent,
            amountReceived,
            spotPriceUSD,
            spotPriceEUR,
            fee,
            feeAssetId,
            feeSpotPriceUSD,
            feeSpotPriceEUR,
            usdEurExchangeRate,
            spotPriceInputCurrency,
            feePriceInputCurrency,
            notes,
            brokerTransactionId);
    }

    public static Transaction CreateDeposit(
        DateTime date,
        decimal amountReceived,
        Guid? toAssetId = null,
        decimal? spotPriceUSD = null,
        decimal? spotPriceEUR = null,
        decimal fee = 0m,
        Guid? feeAssetId = null,
        decimal? feeSpotPriceUSD = null,
        decimal? feeSpotPriceEUR = null,
        decimal? usdEurExchangeRate = null,
        FiatCurrency? spotPriceInputCurrency = null,
        FiatCurrency? feePriceInputCurrency = null,
        string? notes = null,
        string? brokerTransactionId = null)
    {
        return Create(
            date,
            TransactionType.Deposit,
            fromAssetId: null,
            toAssetId: toAssetId ?? FiatCurrency.TaxCurrency.Id,
            amountSpent: 0m,
            amountReceived: amountReceived,
            spotPriceUSD: spotPriceUSD,
            spotPriceEUR: spotPriceEUR,
            fee: fee,
            feeAssetId: feeAssetId,
            feeSpotPriceUSD: feeSpotPriceUSD,
            feeSpotPriceEUR: feeSpotPriceEUR,
            usdEurExchangeRate: usdEurExchangeRate,
            spotPriceInputCurrency: spotPriceInputCurrency,
            feePriceInputCurrency: feePriceInputCurrency,
            notes: notes,
            brokerTransactionId: brokerTransactionId);
    }

    public static Transaction CreateWithdrawal(
        DateTime date,
        decimal amountSpent,
        Guid? fromAssetId = null,
        decimal? spotPriceUSD = null,
        decimal? spotPriceEUR = null,
        decimal fee = 0m,
        Guid? feeAssetId = null,
        decimal? feeSpotPriceUSD = null,
        decimal? feeSpotPriceEUR = null,
        decimal? usdEurExchangeRate = null,
        FiatCurrency? spotPriceInputCurrency = null,
        FiatCurrency? feePriceInputCurrency = null,
        string? notes = null,
        string? brokerTransactionId = null)
    {
        return Create(
            date,
            TransactionType.Withdrawal,
            fromAssetId: fromAssetId ?? FiatCurrency.TaxCurrency.Id,
            toAssetId: null,
            amountSpent: amountSpent,
            amountReceived: 0m,
            spotPriceUSD: spotPriceUSD,
            spotPriceEUR: spotPriceEUR,
            fee: fee,
            feeAssetId: feeAssetId,
            feeSpotPriceUSD: feeSpotPriceUSD,
            feeSpotPriceEUR: feeSpotPriceEUR,
            usdEurExchangeRate: usdEurExchangeRate,
            spotPriceInputCurrency: spotPriceInputCurrency,
            feePriceInputCurrency: feePriceInputCurrency,
            notes: notes,
            brokerTransactionId: brokerTransactionId);
    }

    public static Transaction CreateReward(
        DateTime date,
        Guid toAssetId,
        decimal amountReceived,
        decimal? spotPriceUSD = null,
        decimal? spotPriceEUR = null,
        decimal fee = 0m,
        Guid? feeAssetId = null,
        decimal? feeSpotPriceUSD = null,
        decimal? feeSpotPriceEUR = null,
        decimal? usdEurExchangeRate = null,
        FiatCurrency? spotPriceInputCurrency = null,
        FiatCurrency? feePriceInputCurrency = null,
        string? notes = null,
        string? brokerTransactionId = null)
    {
        return Create(
            date,
            TransactionType.Reward,
            fromAssetId: null,
            toAssetId: toAssetId,
            amountSpent: 0m,
            amountReceived: amountReceived,
            spotPriceUSD: spotPriceUSD,
            spotPriceEUR: spotPriceEUR,
            fee: fee,
            feeAssetId: feeAssetId,
            feeSpotPriceUSD: feeSpotPriceUSD,
            feeSpotPriceEUR: feeSpotPriceEUR,
            usdEurExchangeRate: usdEurExchangeRate,
            spotPriceInputCurrency: spotPriceInputCurrency,
            feePriceInputCurrency: feePriceInputCurrency,
            notes: notes,
            brokerTransactionId: brokerTransactionId);
    }

    public static Transaction CreateLoss(
        DateTime date,
        Guid fromAssetId,
        decimal amountSpent,
        decimal? spotPriceUSD = null,
        decimal? spotPriceEUR = null,
        decimal fee = 0m,
        Guid? feeAssetId = null,
        decimal? feeSpotPriceUSD = null,
        decimal? feeSpotPriceEUR = null,
        decimal? usdEurExchangeRate = null,
        FiatCurrency? spotPriceInputCurrency = null,
        FiatCurrency? feePriceInputCurrency = null,
        string? notes = null,
        string? brokerTransactionId = null)
    {
        return Create(
            date,
            TransactionType.Loss,
            fromAssetId: fromAssetId,
            toAssetId: null,
            amountSpent: amountSpent,
            amountReceived: 0m,
            spotPriceUSD: spotPriceUSD,
            spotPriceEUR: spotPriceEUR,
            fee: fee,
            feeAssetId: feeAssetId,
            feeSpotPriceUSD: feeSpotPriceUSD,
            feeSpotPriceEUR: feeSpotPriceEUR,
            usdEurExchangeRate: usdEurExchangeRate,
            spotPriceInputCurrency: spotPriceInputCurrency,
            feePriceInputCurrency: feePriceInputCurrency,
            notes: notes,
            brokerTransactionId: brokerTransactionId);
    }

    #endregion

    private void SetCoreDetails(
        DateTime date,
        TransactionType transactionType,
        Guid? fromAssetId,
        Guid? toAssetId,
        decimal amountSpent,
        decimal amountReceived,
        decimal? spotPriceUSD,
        decimal? spotPriceEUR,
        decimal fee,
        Guid? feeAssetId,
        decimal? feeSpotPriceUSD,
        decimal? feeSpotPriceEUR,
        FiatCurrency? spotPriceInputCurrency,
        FiatCurrency? feePriceInputCurrency,
        string? notes)
    {
        ValidateInvariants(date, transactionType, fromAssetId, toAssetId);

        Date = date;
        Type = transactionType;
        FromAssetId = fromAssetId;
        ToAssetId = toAssetId;
        AmountSpent = amountSpent;
        AmountReceived = amountReceived;
        Fee = fee;
        FeeAssetId = feeAssetId;

        SpotPrice = TransactionPriceResolver.ResolveSpotPrice(
            transactionType, fromAssetId, toAssetId, amountSpent, amountReceived,
            spotPriceUSD, spotPriceEUR, spotPriceInputCurrency);

        FeePrice = TransactionPriceResolver.ResolveFeePrice(
            feeAssetId, feeSpotPriceUSD, feeSpotPriceEUR, feePriceInputCurrency);

        Notes = notes;
    }

    private static void ValidateInvariants(
        DateTime date,
        TransactionType transactionType,
        Guid? fromAssetId,
        Guid? toAssetId)
    {
        if (date > DateTime.UtcNow)
            throw new ArgumentException("Transaction date cannot be in the future.", nameof(date));

        ArgumentNullException.ThrowIfNull(transactionType);

        if (transactionType.RequiresFromAsset && !fromAssetId.HasValue)
            throw new ArgumentException($"FromAssetId is required for {transactionType.Name}");
        if (!transactionType.RequiresFromAsset && fromAssetId.HasValue)
            throw new ArgumentException($"FromAssetId must be null for {transactionType.Name}");

        if (transactionType.RequiresToAsset && !toAssetId.HasValue)
            throw new ArgumentException($"ToAssetId is required for {transactionType.Name}");
        if (!transactionType.RequiresToAsset && toAssetId.HasValue)
            throw new ArgumentException($"ToAssetId must be null for {transactionType.Name}");

        if (transactionType == TransactionType.Deposit && FiatCurrency.FromIdOrDefault(toAssetId) != FiatCurrency.TaxCurrency)
            throw new ArgumentException($"Deposit must be in the tax currency ({FiatCurrency.TaxCurrency.Symbol}).");

        if (transactionType == TransactionType.Withdrawal && FiatCurrency.FromIdOrDefault(fromAssetId) != FiatCurrency.TaxCurrency)
            throw new ArgumentException($"Withdrawal must be in the tax currency ({FiatCurrency.TaxCurrency.Symbol}).");
    }

    public void UpdateExchangeRates(decimal usdEurRate)
    {
        SpotPrice = SpotPrice.WithDerivedRate(usdEurRate);
        if (FeePrice != null)
        {
            FeePrice = FeePrice.WithDerivedRate(usdEurRate);
        }

        UsdEurExchangeRate = usdEurRate;
    }

    public void Update(
        DateTime date,
        TransactionType transactionType,
        Guid? fromAssetId,
        Guid? toAssetId,
        decimal amountSpent,
        decimal amountReceived,
        decimal? spotPriceUSD,
        decimal? spotPriceEUR,
        decimal fee,
        Guid? feeAssetId,
        decimal? feeSpotPriceUSD,
        decimal? feeSpotPriceEUR,
        FiatCurrency? spotPriceInputCurrency,
        FiatCurrency? feePriceInputCurrency,
        string? notes)
    {
        SetCoreDetails(
            date,
            transactionType,
            fromAssetId,
            toAssetId,
            amountSpent,
            amountReceived,
            spotPriceUSD,
            spotPriceEUR,
            fee,
            feeAssetId,
            feeSpotPriceUSD,
            feeSpotPriceEUR,
            spotPriceInputCurrency,
            feePriceInputCurrency,
            notes);
    }

    public decimal? GetFromAssetPrice(FiatCurrency currency)
    {
        if (!Type.RequiresFromAsset) return null;
        return SpotPrice.GetPrice(currency);
    }

    public decimal? GetToAssetPrice(FiatCurrency currency)
    {
        // For direct acquisitions (Deposit or Reward), SpotPrice directly describes ToAsset.
        bool isDirectAcquisition = !Type.RequiresFromAsset;
        if (isDirectAcquisition)
        {
            return SpotPrice.GetPrice(currency);
        }

        // Transactions without a received asset (Withdrawal or Loss) have no ToAsset price.
        if (!Type.RequiresToAsset) return null;

        // For Swap: derive ToAsset acquisition price from FromAsset price (SpotPrice)
        decimal? fromPrice = GetFromAssetPrice(currency);
        if (!fromPrice.HasValue || AmountReceived == 0) return null;

        return AmountSpent * fromPrice / AmountReceived;
    }

    public decimal? GetFeeAssetPrice(FiatCurrency currency)
    {
        return FeePrice?.GetPrice(currency);
    }
}
