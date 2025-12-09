namespace QueueItUp.Abstractions;

/// <summary>
/// Non-generic base interface for tasks, enabling storage of heterogeneous task collections.
/// </summary>
public interface ITask
{
    string Id { get; }
    Status Status { get; }
}
