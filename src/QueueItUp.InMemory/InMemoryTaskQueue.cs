using QueueItUp.Abstractions;
using System.Collections.Concurrent;

namespace QueueItUp.InMemory;

/// <summary>
/// In-memory implementation of ITaskQueue for fast, ephemeral task management.
/// </summary>
public class InMemoryTaskQueue
{
    private readonly ConcurrentQueue<ITaskImplementation<object, object>> _queue = new();

    public Task EnqueueAsync<TInput, TOutput>(ITaskImplementation<TInput, TOutput> task, CancellationToken cancellationToken)
    {
        _queue.Enqueue((ITaskImplementation<object, object>)task);
        return Task.CompletedTask;
    }

    public Task<ITaskInfo<object, object>?> DequeueAsync(CancellationToken cancellationToken)
    {
        _queue.TryDequeue(out var task);
        return Task.FromResult(task as ITaskInfo<object, object>);
    }
}
