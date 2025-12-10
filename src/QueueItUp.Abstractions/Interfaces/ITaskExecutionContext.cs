namespace QueueItUp.Abstractions;

/// <summary>
/// Provides context for task execution.
/// </summary>
public interface ITaskExecutionContext
{
    /// <summary>
    /// The queue where tasks can be enqueued.
    /// </summary>
    ITaskQueue Queue { get; }
    
    /// <summary>
    /// Executes the next available task from the queue.
    /// </summary>
    Task<ITask?> ExecuteNextAsync(CancellationToken cancellationToken = default);
}
