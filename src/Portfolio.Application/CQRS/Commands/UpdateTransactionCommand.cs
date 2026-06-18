using MediatR;
using Microsoft.Extensions.Logging;
using Portfolio.Application.DTOs;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;

namespace Portfolio.Application.CQRS.Commands;

public record UpdateTransactionCommand(int Id, NewTransactionRequest Request) : IRequest<TransactionDto>;

public class UpdateTransactionCommandHandler(IUnitOfWork uow, ILogger<UpdateTransactionCommandHandler> logger)
    : IRequestHandler<UpdateTransactionCommand, TransactionDto>
{
    public async Task<TransactionDto> Handle(UpdateTransactionCommand command, CancellationToken cancellationToken)
    {
        var transaction = await uow.Transactions.GetByIdAsync(command.Id) 
                          ?? throw new KeyNotFoundException($"Transaction {command.Id} not found.");

        var type = TransactionType.FromValue(command.Request.TransactionTypeCode) 
                   ?? throw new ArgumentException($"Invalid transaction type: {command.Request.TransactionTypeCode}");

        FiatCurrency? spotInputCurrency = null;
        if (!string.IsNullOrEmpty(command.Request.SpotPriceInputCurrency))
        {
            spotInputCurrency = FiatCurrency.Parse(command.Request.SpotPriceInputCurrency) 
                                ?? throw new ArgumentException($"Invalid fiat currency: {command.Request.SpotPriceInputCurrency}");
        }

        FiatCurrency? feeInputCurrency = null;
        if (!string.IsNullOrEmpty(command.Request.FeePriceInputCurrency))
        {
            feeInputCurrency = FiatCurrency.Parse(command.Request.FeePriceInputCurrency) 
                               ?? throw new ArgumentException($"Invalid fiat currency: {command.Request.FeePriceInputCurrency}");
        }

        transaction.Update(
            date: command.Request.Date,
            transactionType: type,
            fromAssetId: command.Request.FromAssetId,
            toAssetId: command.Request.ToAssetId,
            amountSpent: command.Request.AmountSpent,
            amountReceived: command.Request.AmountReceived,
            spotPriceUSD: command.Request.SpotPriceUSD,
            spotPriceEUR: command.Request.SpotPriceEUR,
            fee: command.Request.Fee,
            feeAssetId: command.Request.FeeAssetId,
            feeSpotPriceUSD: command.Request.FeePriceUSD,
            feeSpotPriceEUR: command.Request.FeePriceEUR,
            spotPriceInputCurrency: spotInputCurrency,
            feePriceInputCurrency: feeInputCurrency,
            notes: command.Request.Notes
        );

        await uow.Transactions.UpdateAsync(transaction);
        await uow.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Updated transaction {TransactionId}.", transaction.Id);

        return new TransactionDto
        {
            Id = transaction.Id,
            Date = transaction.Date,
            Type = transaction.Type,
            FromAsset = transaction.FromAssetId.HasValue ? new AssetDto { Id = transaction.FromAssetId.Value } : null,
            ToAsset = transaction.ToAssetId.HasValue ? new AssetDto { Id = transaction.ToAssetId.Value } : null,
            AmountSpent = transaction.AmountSpent,
            AmountReceived = transaction.AmountReceived,
            SpotPriceUSD = transaction.SpotPriceUSD,
            SpotPriceEUR = transaction.SpotPriceEUR,
            Fee = transaction.Fee,
            FeeAsset = transaction.FeeAssetId.HasValue ? new AssetDto { Id = transaction.FeeAssetId.Value } : null,
            FeePriceUSD = transaction.FeePriceUSD,
            FeePriceEUR = transaction.FeePriceEUR,
            UsdEurExchangeRate = transaction.UsdEurExchangeRate,
            Notes = transaction.Notes
        };
    }
}
