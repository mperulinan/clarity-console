using MediatR;

namespace Portfolio.Application.Interfaces;

public interface IBackgroundTaskQueue
{
    ValueTask QueueBackgroundWorkItemAsync(IRequest workItem);
    ValueTask<IRequest> DequeueAsync(CancellationToken cancellationToken);
}
