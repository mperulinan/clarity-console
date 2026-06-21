using MediatR;
using Microsoft.Extensions.Logging;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using Portfolio.Application.Mappers;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;

namespace Portfolio.Application.CQRS.Commands;

public class AddTransactionCommandHandler(
    IUnitOfWork uow, 
    IBackgroundTaskQueue backgroundQueue,
    ILogger<AddTransactionCommandHandler> logger)
    : IRequestHandler<AddTransactionCommand, TransactionDto>
{
    public async Task<TransactionDto> Handle(AddTransactionCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;
        logger.LogInformation("Adding new {Type} transaction on {Date}", request.TransactionTypeCode.Name, request.Date);

        var transaction = new Transaction(
            request.Date,
            request.TransactionTypeCode,
            request.FromAssetId,
            request.ToAssetId,
            request.AmountSpent,
            request.AmountReceived,
            request.SpotPriceUSD,
            request.SpotPriceEUR,
            request.Fee,
            request.FeeAssetId,
            request.FeePriceUSD,
            request.FeePriceEUR,
            usdEurExchangeRate: null, // UsdEurExchangeRate is null initially
            request.SpotPriceInputCurrency != null ? FiatCurrency.Parse(request.SpotPriceInputCurrency) : null,
            request.FeePriceInputCurrency != null ? FiatCurrency.Parse(request.FeePriceInputCurrency) : null,
            request.Notes
        );

        await uow.Transactions.AddAsync(transaction);
        await uow.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Successfully added transaction {TransactionId}.", transaction.Id);

        // Queue a background sync job so the Spot Price updates immediately
        await backgroundQueue.QueueBackgroundWorkItemAsync(new SyncTransactionExchangeRateCommand(transaction.Id));
        return TransactionMapper.ToDto(transaction);
    }
}
