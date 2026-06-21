using System.Threading.Channels;
using MediatR;
using Portfolio.Application.Interfaces;

namespace Portfolio.Application.Services;

public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<IRequest> _queue;

    public BackgroundTaskQueue()
    {
        _queue = Channel.CreateUnbounded<IRequest>();
    }

    public async ValueTask QueueBackgroundWorkItemAsync(IRequest workItem)
    {
        ArgumentNullException.ThrowIfNull(workItem);
        await _queue.Writer.WriteAsync(workItem);
    }

    public async ValueTask<IRequest> DequeueAsync(CancellationToken cancellationToken)
    {
        return await _queue.Reader.ReadAsync(cancellationToken);
    }
}
