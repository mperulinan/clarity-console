using MediatR;
using Portfolio.Application.DTOs;
using Portfolio.Application.Mappers;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Interfaces;

namespace Portfolio.Application.CQRS.Queries;

public class GetProcessedTransactionByIdQueryHandler(
    IUnitOfWork unitOfWork,
    IInventoryCalculator inventoryCalculator) 
    : IRequestHandler<GetProcessedTransactionByIdQuery, ProcessedTransactionDto?>
{
    public async Task<ProcessedTransactionDto?> Handle(GetProcessedTransactionByIdQuery query, CancellationToken cancellationToken)
    {
        var transactions = await unitOfWork.Transactions.GetAllAsync();
        
        // Ensure the transaction actually exists before calculating
        if (!transactions.Any(t => t.Id == query.Id))
            return null;

        var report = inventoryCalculator.CalculateInventory(transactions, FiatCurrency.TaxCurrency);
        var processedTransaction = report.Transactions.FirstOrDefault(pt => pt.Transaction.Id == query.Id);
        
        return processedTransaction?.ToDto();
    }
}
