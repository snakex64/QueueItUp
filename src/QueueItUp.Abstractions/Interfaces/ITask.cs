namespace QueueItUp.Abstractions;

/// <summary>
/// Non-generic base interface for tasks, enabling storage of heterogeneous task collections.
/// </summary>
public interface ITask
{
    string Id { get; }
    Status Status { get; }
    
    /// <summary>
    /// The ID of the parent task, if this task is a sub-task.
    /// </summary>
    string? ParentTaskId { get; }
    
    /// <summary>
    /// Gets the IDs of all sub-tasks that have been created by this task.
    /// </summary>
    IReadOnlyList<string> SubTaskIds { get; }
}
