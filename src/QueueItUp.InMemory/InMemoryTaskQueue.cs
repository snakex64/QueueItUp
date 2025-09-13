using QueueItUp.Abstractions;
using System.Collections.Concurrent;

namespace QueueItUp.InMemory;

/// <summary>
/// In-memory implementation of ITaskQueue for fast, ephemeral task management.
/// </summary>
public class InMemoryTaskQueue : ITaskQueue
{
    private readonly ConcurrentQueue<ITask<object, object>> _queue = new();

    public Task EnqueueAsync<TInput, TOutput>(ITask<TInput, TOutput> task, CancellationToken cancellationToken = default)
    {
        _queue.Enqueue((ITask<object, object>)task);
        return Task.CompletedTask;
    }

    public Task<ITask<object, object>?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        _queue.TryDequeue(out var task);
        return Task.FromResult(task);
    }
}
