using MediatR;
using Portfolio.Application.Interfaces;

namespace Portfolio.API.BackgroundServices;

public class QueuedHostedService(
    IBackgroundTaskQueue taskQueue,
    IServiceScopeFactory scopeFactory,
    ILogger<QueuedHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("QueuedHostedService is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var workItem = await taskQueue.DequeueAsync(stoppingToken);

                using var scope = scopeFactory.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                
                logger.LogInformation("Processing background work item: {WorkItemType}", workItem.GetType().Name);
                await mediator.Send(workItem, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Prevent throwing if the task was cancelled gracefully
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred executing background work item.");
            }
        }

        logger.LogInformation("QueuedHostedService is stopping.");
    }
}
