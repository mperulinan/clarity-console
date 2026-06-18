using Portfolio.Application.DTOs;
using Portfolio.Domain.Entities;
using Portfolio.Domain.ValueObjects;

namespace Portfolio.Application.Mappers;

public static class TransactionMapper
{
    public static TransactionDto ToDto(this Transaction transaction) =>
        new()
        {
            Id = transaction.Id,
            Date = transaction.Date,
            Type = transaction.Type,
            FromAsset = transaction.FromAsset?.ToDto(),
            ToAsset = transaction.ToAsset?.ToDto(),
            AmountSpent = transaction.AmountSpent,
            AmountReceived = transaction.AmountReceived,
            SpotPriceUSD = transaction.SpotPriceUSD,
            SpotPriceEUR = transaction.SpotPriceEUR,
            SpotPriceInputCurrency = transaction.SpotPriceInputCurrency?.Value,
            Fee = transaction.Fee,
            FeeAsset = transaction.FeeAsset?.ToDto(),
            FeePriceUSD = transaction.FeePriceUSD,
            FeePriceEUR = transaction.FeePriceEUR,
            FeePriceInputCurrency = transaction.FeePriceInputCurrency?.Value,
            UsdEurExchangeRate = transaction.UsdEurExchangeRate,
            Notes = transaction.Notes
        };

    public static ProcessedTransactionDto ToDto(this ProcessedTransaction pt) =>
        new()
        {
            Transaction = pt.Transaction.ToDto(),
            ProfitLoss = pt.ProfitLoss,
            TotalLossAmount = pt.TotalLossAmount,
            IsLossDisallowed = pt.IsLossDisallowed,
            DisallowedByTransactionId = pt.DisallowedByTransactionId,
            DisallowsPreviousLosses = pt.DisallowsPreviousLosses,
            Error = pt.Error
        };
}
