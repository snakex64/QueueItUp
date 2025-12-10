using QueueItUp.Abstractions;

namespace QueueItUp.Core;

/// <summary>
/// Default implementation of ITaskExecutionContext.
/// </summary>
public class TaskExecutionContext : ITaskExecutionContext
{
    public TaskExecutionContext(ITaskQueue queue)
    {
        Queue = queue;
    }
    
    public ITaskQueue Queue { get; }

    public async Task<ITask?> ExecuteNextAsync(CancellationToken cancellationToken = default)
    {
        var task = await Queue.DequeueAsync(cancellationToken);
        if (task == null)
        {
            return null;
        }
        
        // Execute the task
        if (task is ITaskExecutable executable)
        {
            await executable.ExecuteAsync(this, cancellationToken);
        }
        
        // Mark as completed
        Queue.MarkTaskCompleted(task.Id, success: true);
        
        return task;
    }
}
