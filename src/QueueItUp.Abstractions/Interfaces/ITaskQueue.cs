namespace QueueItUp.Abstractions;

/// <summary>
/// Represents a queue for managing tasks.
/// </summary>
public interface ITaskQueue
{
    /// <summary>
    /// Enqueues a task for later execution.
    /// </summary>
    Task EnqueueAsync<TInput, TOutput>(ITaskImplementation<TInput, TOutput> task, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Dequeues the next available task.
    /// </summary>
    Task<ITask?> DequeueAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets information about a specific task by ID, including completed tasks.
    /// </summary>
    ITask? GetTaskInfo(string taskId);
    
    /// <summary>
    /// Marks a task as completed. This allows dependent tasks to be dequeued.
    /// </summary>
    void MarkTaskCompleted(string taskId, bool success = true);
}
