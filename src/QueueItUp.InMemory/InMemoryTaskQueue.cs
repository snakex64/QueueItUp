using QueueItUp.Abstractions;
using System.Collections.Concurrent;

namespace QueueItUp.InMemory;

/// <summary>
/// In-memory implementation of ITaskQueue for fast, ephemeral task management.
/// </summary>
public class InMemoryTaskQueue : ITaskQueue
{
    private readonly ConcurrentQueue<ITask> _queue = new();

    public Task EnqueueAsync<TInput, TOutput>(ITaskImplementation<TInput, TOutput> task, CancellationToken cancellationToken = default)
    {
        _queue.Enqueue(task);
        return Task.CompletedTask;
    }

    public Task<ITask?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        _queue.TryDequeue(out var task);
        return Task.FromResult(task);
    }
}
