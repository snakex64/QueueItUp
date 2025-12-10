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
    /// Enqueues a sub-task for execution. The sub-task will be linked to the specified parent task.
    /// </summary>
    Task EnqueueSubTaskAsync<TInput, TOutput>(ITaskImplementation<TInput, TOutput> subTask, string parentTaskId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Enqueues a next task that will execute after the specified task completes.
    /// The next task will have a dependency on the specified task.
    /// </summary>
    Task EnqueueNextTaskAsync<TInput, TOutput>(ITaskImplementation<TInput, TOutput> nextTask, string afterTaskId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Dequeues the next available task.
    /// </summary>
    Task<ITask?> DequeueAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Tries to get information about a specific task by ID, including completed tasks.
    /// </summary>
    bool TryGetTaskInfo(string taskId, out ITask? task);
    
    /// <summary>
    /// Tries to get information about a task by name.
    /// </summary>
    bool TryGetTaskInfoByName(string taskName, out ITask? task);
    
    /// <summary>
    /// Marks a task as completed. This allows dependent tasks to be dequeued.
    /// </summary>
    void MarkTaskCompleted(string taskId, bool success = true);
}
