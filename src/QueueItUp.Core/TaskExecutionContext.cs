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

    public async Task<string> EnqueueNextTaskAsync<TInput, TOutput>(ITaskImplementation<TInput, TOutput> nextTask, CancellationToken cancellationToken = default)
    {
        // The next task depends on the current task and all its sub-tasks
        if (nextTask is ITaskWithSubTasks nextTaskWithSubTasks)
        {
            // Add dependency on current task
            nextTaskWithSubTasks.AddDependencyTaskId(CurrentTaskId);
            
            // Add dependencies on all sub-tasks of current task
            foreach (var subTaskId in _currentTask.SubTaskIds)
            {
                nextTaskWithSubTasks.AddDependencyTaskId(subTaskId);
            }
        }
        
        // Enqueue the next task
        await Queue.EnqueueAsync(nextTask, cancellationToken);
        
        return nextTask.Id;
    }
}
