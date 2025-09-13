namespace QueueItUp.Abstractions;

/// <summary>
/// Represents a generic asynchronous task with typed input and output.
/// </summary>
public interface ITask<TInput, TOutput> : ITaskInfo<TInput, TOutput>
{
    Task<TOutput> ExecuteAsync(CancellationToken cancellationToken);
}