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
}
