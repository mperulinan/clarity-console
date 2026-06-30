using MediatR;
using Portfolio.Application.DTOs;
using Portfolio.Application.Mappers;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;

namespace Portfolio.Application.CQRS.Queries;

public class GetProcessedTransactionsQueryHandler(
    IUnitOfWork unitOfWork,
    IInventoryCalculator inventoryCalculator)
    : IRequestHandler<GetProcessedTransactionsQuery, IEnumerable<ProcessedTransactionDto>>
{
    public async Task<IEnumerable<ProcessedTransactionDto>> Handle(
        GetProcessedTransactionsQuery query,
        CancellationToken cancellationToken)
    {
        var currency = query.Currency is not null
            ? FiatCurrency.Parse(query.Currency)
            : FiatCurrency.TaxCurrency;

        var transactions = await unitOfWork.Transactions.GetAllAsync();
        var report = inventoryCalculator.CalculateInventory(transactions, currency);

        return report.Transactions.Select(pt => pt.ToDto());
    }
}
