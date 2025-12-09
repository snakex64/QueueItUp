namespace QueueItUp.Abstractions;

/// <summary>
/// Provides context for task execution, including the ability to enqueue sub-tasks.
/// </summary>
public interface ITaskExecutionContext
{
    /// <summary>
    /// The ID of the currently executing task.
    /// </summary>
    string CurrentTaskId { get; }
    
    /// <summary>
    /// The queue where sub-tasks can be enqueued.
    /// </summary>
    ITaskQueue Queue { get; }
    
    /// <summary>
    /// Enqueues a sub-task for execution. The sub-task will be linked to the current task.
    /// </summary>
    Task<string> EnqueueSubTaskAsync<TInput, TOutput>(ITaskImplementation<TInput, TOutput> subTask, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Enqueues a next task that will execute after the current task and all its sub-tasks complete.
    /// The next task will have a dependency on the current task and all its sub-tasks.
    /// </summary>
    Task<string> EnqueueNextTaskAsync<TInput, TOutput>(ITaskImplementation<TInput, TOutput> nextTask, CancellationToken cancellationToken = default);
}
