namespace QueueItUp.Abstractions;

/// <summary>
/// Possible lifecycle states of a task.
/// </summary>
public enum Status
{
    Pending,
    Running,
    Completed,
    Failed
}