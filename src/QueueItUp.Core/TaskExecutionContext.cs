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
        
        bool success = true;
        try
        {
            // Execute the task
            if (task is ITaskExecutable executable)
            {
                await executable.ExecuteAsync(this, cancellationToken);
            }
        }
        catch
        {
            success = false;
            throw;
        }
        finally
        {
            // Mark as completed or failed
            Queue.MarkTaskCompleted(task.Id, success: success);
        }
        
        return task;
    }
}
