namespace QueueItUp.Abstractions;

/// <summary>
/// Non-generic interface for task execution.
/// Allows executing tasks without knowing their specific generic types.
/// </summary>
public interface ITaskExecutable : ITask
{
    /// <summary>
    /// Executes the task with the provided execution context.
    /// Returns a Task that can be awaited even though we don't know the generic result type.
    /// </summary>
    Task ExecuteAsync(ITaskExecutionContext context, CancellationToken cancellationToken = default);
}
