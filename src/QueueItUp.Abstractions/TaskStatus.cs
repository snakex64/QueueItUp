namespace QueueItUp.Abstractions;

/// <summary>
/// Possible lifecycle states of a task.
/// </summary>
public enum TaskStatus
{
    Pending,
    Running,
    Completed,
    Failed
}