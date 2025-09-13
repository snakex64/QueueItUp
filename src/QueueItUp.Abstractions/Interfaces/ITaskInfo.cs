namespace QueueItUp.Abstractions;

/// <summary>
/// Represents a read-only view of a task for inspection purposes.
/// </summary>
public interface ITaskInfo<TInput, TOutput>
{
    string Id { get; }

    TaskStatus Status { get; }

    Task<TInput> LoadInputAsync(CancellationToken cancellationToken);

    Task<TOutput> LoadOutputAsync(CancellationToken cancellationToken);
}