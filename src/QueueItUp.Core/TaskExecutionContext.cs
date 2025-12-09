using QueueItUp.Abstractions;

namespace QueueItUp.Core;

/// <summary>
/// Default implementation of ITaskExecutionContext.
/// </summary>
public class TaskExecutionContext : ITaskExecutionContext
{
    private readonly ITask _currentTask;

    public TaskExecutionContext(ITask currentTask, ITaskQueue queue)
    {
        _currentTask = currentTask;
        Queue = queue;
    }

    public string CurrentTaskId => _currentTask.Id;
    
    public ITaskQueue Queue { get; }

    public async Task<string> EnqueueSubTaskAsync<TInput, TOutput>(ITaskImplementation<TInput, TOutput> subTask, CancellationToken cancellationToken = default)
    {
        // Set the parent-child relationship on the sub-task
        if (subTask is ITaskWithSubTasks subTaskWithSubTasks)
        {
            subTaskWithSubTasks.SetParentTaskId(CurrentTaskId);
        }
        
        // Register the sub-task with the parent
        if (_currentTask is ITaskWithSubTasks parentTaskWithSubTasks)
        {
            parentTaskWithSubTasks.AddSubTaskId(subTask.Id);
        }
        
        // Enqueue the sub-task
        await Queue.EnqueueAsync(subTask, cancellationToken);
        
        return subTask.Id;
    }
}
