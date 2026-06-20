using MediatR;
using Microsoft.Extensions.Logging;
using Portfolio.Domain.Interfaces;

namespace Portfolio.Application.CQRS.Commands;

public class CalculateExchangeRatesCommandHandler(
    IUnitOfWork uow, 
    IExchangeRateProvider exchangeRateProvider,
    ILogger<CalculateExchangeRatesCommandHandler> logger)
    : IRequestHandler<CalculateExchangeRatesCommand>
{
    public async Task Handle(CalculateExchangeRatesCommand command, CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting background calculation for missing exchange rates.");
        
        var allTransactions = await uow.Transactions.GetAllAsync();
        var transactions = allTransactions.Where(t => t.HasIncompleteExchangeRates).ToList();

        if (transactions.Count == 0)
        {
            logger.LogInformation("No transactions found requiring exchange rate updates.");
            return;
        }

        logger.LogInformation("Found {Count} transactions requiring updates.", transactions.Count);

        var dates = transactions.Select(t => t.Date.Date).Distinct().ToList();
        var ratesByDate = new Dictionary<DateTime, decimal>();

        foreach (var date in dates)
        {
            try
            {
                var rate = await exchangeRateProvider.GetUsdEurRateAsync(date);
                ratesByDate[date] = rate;
            }
            catch (NotSupportedException)
            {
                logger.LogInformation("Exchange rate for date {Date} is not yet available. Skipping.", date);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to fetch exchange rate for date {Date}", date);
            }
        }

        foreach (var tx in transactions)
        {
            if (ratesByDate.TryGetValue(tx.Date.Date, out decimal rate))
            {
                tx.UpdateExchangeRates(rate);
            }
        }

        await uow.SaveChangesAsync();
        logger.LogInformation("Finished updating exchange rates.");
    }
}
