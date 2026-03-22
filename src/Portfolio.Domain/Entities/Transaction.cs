using Portfolio.Domain.Enums;

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
    public decimal? SpotPriceUSD { get; private set; }
    public decimal? SpotPriceEUR { get; private set; }
    public decimal Fee { get; private set; }
    public Guid? FeeAssetId { get; private set; }
    public Asset? FeeAsset { get; set; }
    public decimal? FeePriceUSD { get; private set; }
    public decimal? FeePriceEUR { get; private set; }
    public decimal? UsdEurExchangeRate { get; private set; }
    public FiatCurrency SpotPriceInputCurrency { get; private set; } = null!;
    public FiatCurrency? FeePriceInputCurrency { get; private set; }
    public string? Notes { get; private set; }

    // Constructor for EF Core.
    private Transaction() { }

    public Transaction(
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
        decimal? usdEurExchangeRate,
        FiatCurrency? spotPriceInputCurrency,
        FiatCurrency? feePriceInputCurrency,
        string? notes)
    {
        Date = date;
        Type = transactionType ?? throw new ArgumentNullException(nameof(transactionType));

        if (Type.RequiresFromAsset && !fromAssetId.HasValue)
            throw new ArgumentException($"FromAssetId is required for {Type.Name}");
        if (!Type.RequiresFromAsset && fromAssetId.HasValue)
            throw new ArgumentException($"FromAssetId must be null for {Type.Name}");
            
        if (Type.RequiresToAsset && !toAssetId.HasValue)
            throw new ArgumentException($"ToAssetId is required for {Type.Name}");
        if (!Type.RequiresToAsset && toAssetId.HasValue)
            throw new ArgumentException($"ToAssetId must be null for {Type.Name}");

        FromAssetId = fromAssetId;
        ToAssetId = toAssetId;
        AmountSpent = amountSpent;
        AmountReceived = amountReceived;
        Fee = fee;
        FeeAssetId = feeAssetId;

        ProcessFiatDerivations(ref spotPriceUSD, ref spotPriceEUR, ref spotPriceInputCurrency);
        ProcessFeeFiatDerivations(ref feeSpotPriceUSD, ref feeSpotPriceEUR, ref feePriceInputCurrency);

        if (spotPriceInputCurrency == null)
            throw new ArgumentException("SpotPriceInputCurrency must be provided if the transaction does not implicitly involve a Fiat currency.");

        if (!spotPriceUSD.HasValue && !spotPriceEUR.HasValue)
            throw new ArgumentException("Either SpotPriceUSD or SpotPriceEUR must logically have a value depending on the Input Currency.");

        SpotPriceUSD = spotPriceUSD;
        SpotPriceEUR = spotPriceEUR;
        FeePriceUSD = feeSpotPriceUSD;
        FeePriceEUR = feeSpotPriceEUR;
        UsdEurExchangeRate = usdEurExchangeRate;
        SpotPriceInputCurrency = spotPriceInputCurrency;
        FeePriceInputCurrency = feePriceInputCurrency;
        Notes = notes;
    }

    private void ProcessFiatDerivations(ref decimal? spotPriceUSD, ref decimal? spotPriceEUR, ref FiatCurrency? spotPriceInputCurrency)
    {
        bool pricesFromAsset = Type.RequiresFromAsset;
        Guid? pricedAssetId = pricesFromAsset ? FromAssetId : ToAssetId;

        var pricedFiat = FiatCurrency.FromIdOrDefault(pricedAssetId);
        if (pricedFiat != null)
        {
            spotPriceInputCurrency = pricedFiat;
            if (pricedFiat == FiatCurrency.USD) spotPriceUSD = 1m;
            if (pricedFiat == FiatCurrency.EUR) spotPriceEUR = 1m;
        }
        else if (pricesFromAsset && ToAssetId.HasValue)
        {
            var toFiat = FiatCurrency.FromIdOrDefault(ToAssetId);
            if (toFiat != null)
            {
                spotPriceInputCurrency = toFiat;
                decimal derivedPrice = AmountSpent > 0 ? AmountReceived / AmountSpent : 0m;
                
                if (toFiat == FiatCurrency.USD) spotPriceUSD = derivedPrice;
                if (toFiat == FiatCurrency.EUR) spotPriceEUR = derivedPrice;
            }
        }
    }

    private void ProcessFeeFiatDerivations(ref decimal? feeSpotPriceUSD, ref decimal? feeSpotPriceEUR, ref FiatCurrency? feePriceInputCurrency)
    {
        var feeFiat = FiatCurrency.FromIdOrDefault(FeeAssetId);
        if (feeFiat != null)
        {
            feePriceInputCurrency = feeFiat;
            feeSpotPriceUSD = feeFiat == FiatCurrency.USD ? 1m : feeSpotPriceUSD;
            feeSpotPriceEUR = feeFiat == FiatCurrency.EUR ? 1m : feeSpotPriceEUR;
        }
    }

    public void UpdateExchangeRates(decimal usdEurRate)
    {
        if (SpotPriceInputCurrency == FiatCurrency.USD && SpotPriceUSD.HasValue)
        {
            SpotPriceEUR = SpotPriceUSD.Value * usdEurRate;
        }
        else if (SpotPriceInputCurrency == FiatCurrency.EUR && SpotPriceEUR.HasValue && usdEurRate > 0)
        {
            SpotPriceUSD ??= SpotPriceEUR.Value / usdEurRate;
        }

        if (FeePriceInputCurrency == FiatCurrency.USD && FeePriceUSD.HasValue && !FeePriceEUR.HasValue)
        {
            FeePriceEUR = FeePriceUSD.Value * usdEurRate;
        }
        else if (FeePriceInputCurrency == FiatCurrency.EUR && FeePriceEUR.HasValue && usdEurRate > 0 && !FeePriceUSD.HasValue)
        {
            FeePriceUSD = FeePriceEUR.Value / usdEurRate;
        }

        UsdEurExchangeRate = usdEurRate;
    }

    public decimal? GetFromAssetPrice(FiatCurrency currency)
    {
        if (Type == TransactionType.Reward || Type == TransactionType.Deposit) return null;

        if (currency == FiatCurrency.USD) return SpotPriceUSD;
        if (currency == FiatCurrency.EUR) return SpotPriceEUR;
        throw new ArgumentException($"Unsupported currency: {currency.Name}");
    }

    public decimal? GetToAssetPrice(FiatCurrency currency)
    {
        if (Type == TransactionType.Reward || Type == TransactionType.Deposit)
        {
            if (currency == FiatCurrency.USD) return SpotPriceUSD;
            if (currency == FiatCurrency.EUR) return SpotPriceEUR;
            return null;
        }

        if (Type == TransactionType.Withdrawal) return null;

        // For Swap: derive ToAsset price from FromAsset price (SpotPrice)
        decimal? fromPrice = GetFromAssetPrice(currency);
        if (!fromPrice.HasValue || AmountReceived == 0) return null;

        return AmountSpent * fromPrice / AmountReceived;
    }

    public decimal? GetFeeAssetPrice(FiatCurrency currency)
    {
        if (currency == FiatCurrency.USD) return FeePriceUSD;
        if (currency == FiatCurrency.EUR) return FeePriceEUR;
        throw new ArgumentException($"Unsupported currency: {currency.Name}");
    }
}
