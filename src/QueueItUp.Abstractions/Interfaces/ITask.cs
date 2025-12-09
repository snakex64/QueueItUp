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
    
    /// <summary>
    /// Gets the IDs of tasks that must complete before this task can be executed.
    /// </summary>
    IReadOnlyList<string> DependencyTaskIds { get; }
    
    /// <summary>
    /// Sets the parent task ID.
    /// </summary>
    void SetParentTaskId(string parentTaskId);
    
    /// <summary>
    /// Adds a sub-task ID to this task's collection.
    /// </summary>
    void AddSubTaskId(string subTaskId);
    
    /// <summary>
    /// Adds a dependency task ID that must complete before this task can execute.
    /// </summary>
    void AddDependencyTaskId(string dependencyTaskId);
    
    /// <summary>
    /// Updates the task status.
    /// </summary>
    void SetStatus(Status status);
}
