using System.Threading;
using System.Threading.Tasks;

namespace QueueItUp.Abstractions;

/// <summary>
/// Abstraction for a task queue, supporting enqueueing and dequeuing of tasks.
/// </summary>
public interface ITaskQueue
{
    Task EnqueueAsync<TInput, TOutput>(ITask<TInput, TOutput> task, CancellationToken cancellationToken = default);
    Task<ITask<object, object>?> DequeueAsync(CancellationToken cancellationToken = default);
}
