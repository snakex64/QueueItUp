namespace QueueItUp.Abstractions;

/// <summary>
/// Possible lifecycle states of a task.
/// </summary>
public enum Status
{
    New,
    Queued,
    SentToRunner,
    WaitingOnDependencies,
    Running,
    Completed,
    Failed,
    Canceled
}