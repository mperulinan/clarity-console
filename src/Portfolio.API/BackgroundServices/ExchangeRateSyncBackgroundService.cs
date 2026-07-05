using MediatR;
using Portfolio.Application.CQRS.Commands;

namespace Portfolio.API.BackgroundServices;

public class ExchangeRateSyncBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<ExchangeRateSyncBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ExchangeRateSyncBackgroundService is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                await mediator.Send(new CalculateExchangeRatesCommand(), stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred executing CalculateExchangeRatesCommand in background service.");
            }

            // Run every 8 hours
            await Task.Delay(TimeSpan.FromHours(8), stoppingToken);
        }

        logger.LogInformation("ExchangeRateSyncBackgroundService is stopping.");
    }
}
