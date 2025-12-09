using QueueItUp.Abstractions;
using System.Collections.Concurrent;

namespace QueueItUp.InMemory;

/// <summary>
/// In-memory implementation of ITaskQueue for fast, ephemeral task management.
/// Supports dependency-based task ordering - tasks are only dequeued when all their dependencies are completed.
/// </summary>
public class InMemoryTaskQueue : ITaskQueue
{
    private readonly ConcurrentQueue<ITask> _queue = new();
    private readonly ConcurrentDictionary<string, ITask> _allTasks = new();
    private readonly ConcurrentDictionary<string, Status> _taskStatuses = new();

    public Task EnqueueAsync<TInput, TOutput>(ITaskImplementation<TInput, TOutput> task, CancellationToken cancellationToken = default)
    {
        _queue.Enqueue(task);
        _allTasks[task.Id] = task;
        _taskStatuses[task.Id] = task.Status;
        
        // Set status to Queued
        if (task is ITaskWithSubTasks taskWithSubTasks)
        {
            taskWithSubTasks.SetStatus(Status.Queued);
            _taskStatuses[task.Id] = Status.Queued;
        }
        
        return Task.CompletedTask;
    }

    public Task<ITask?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        // Try to find a task whose dependencies are all completed
        var tempQueue = new List<ITask>();
        ITask? taskToReturn = null;
        
        while (_queue.TryDequeue(out var task))
        {
            if (AreDependenciesMet(task))
            {
                taskToReturn = task;
                
                // Set status to SentToRunner
                if (task is ITaskWithSubTasks taskWithSubTasks)
                {
                    taskWithSubTasks.SetStatus(Status.SentToRunner);
                    _taskStatuses[task.Id] = Status.SentToRunner;
                }
                
                break;
            }
            else
            {
                // Dependencies not met, put it aside temporarily
                tempQueue.Add(task);
                
                // Update status to WaitingOnDependencies if not already set
                if (task is ITaskWithSubTasks taskWithSubTasks && task.Status != Status.WaitingOnDependencies)
                {
                    taskWithSubTasks.SetStatus(Status.WaitingOnDependencies);
                    _taskStatuses[task.Id] = Status.WaitingOnDependencies;
                }
            }
        }
        
        // Re-enqueue tasks we couldn't execute yet
        foreach (var task in tempQueue)
        {
            _queue.Enqueue(task);
        }
        
        return Task.FromResult(taskToReturn);
    }

    /// <summary>
    /// Marks a task as completed. This allows dependent tasks to be dequeued.
    /// </summary>
    public void MarkTaskCompleted(string taskId, bool success = true)
    {
        var status = success ? Status.Completed : Status.Failed;
        _taskStatuses[taskId] = status;
        
        if (_allTasks.TryGetValue(taskId, out var task) && task is ITaskWithSubTasks taskWithSubTasks)
        {
            taskWithSubTasks.SetStatus(status);
        }
    }

    private bool AreDependenciesMet(ITask task)
    {
        // If no dependencies, it can be executed
        if (task.DependencyTaskIds.Count == 0)
        {
            return true;
        }
        
        // Check if all dependencies are completed
        foreach (var dependencyId in task.DependencyTaskIds)
        {
            if (!_taskStatuses.TryGetValue(dependencyId, out var status) || status != Status.Completed)
            {
                return false;
            }
        }
        
        return true;
    }
}
