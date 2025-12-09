namespace QueueItUp.Abstractions;

/// <summary>
/// Non-generic interface for managing sub-tasks.
/// This allows manipulation of sub-tasks without knowing the specific generic types.
/// </summary>
public interface ITaskWithSubTasks : ITask
{
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
