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
        subTask.SetParentTaskId(CurrentTaskId);
        
        // Register the sub-task with the parent
        _currentTask.AddSubTaskId(subTask.Id);
        
        // Enqueue the sub-task
        await Queue.EnqueueAsync(subTask, cancellationToken);
        
        return subTask.Id;
    }

    public async Task<string> EnqueueNextTaskAsync<TInput, TOutput>(ITaskImplementation<TInput, TOutput> nextTask, CancellationToken cancellationToken = default)
    {
        // The next task depends only on the current task, NOT on sub-tasks
        // Sub-tasks can execute in parallel/any order, so next task only waits for the parent
        nextTask.AddDependencyTaskId(CurrentTaskId);
        
        // Enqueue the next task
        await Queue.EnqueueAsync(nextTask, cancellationToken);
        
        return nextTask.Id;
    }
}
