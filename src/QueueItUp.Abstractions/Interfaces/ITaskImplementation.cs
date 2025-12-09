namespace QueueItUp.Abstractions;

/// <summary>
/// Represents a generic asynchronous task with typed input and output.
/// </summary>
public interface ITaskImplementation<TInput, TOutput> : ITaskInfo<TInput, TOutput>
{
    /// <summary>
    /// Executes the task. The context provides access to enqueue sub-tasks.
    /// </summary>
    Task<TOutput> ExecuteAsync(ITaskExecutionContext context, CancellationToken cancellationToken);
}