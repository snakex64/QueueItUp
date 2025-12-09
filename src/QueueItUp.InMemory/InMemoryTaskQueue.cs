using QueueItUp.Abstractions;
using System.Collections.Concurrent;

namespace QueueItUp.InMemory;

/// <summary>
/// In-memory implementation of ITaskQueue for fast, ephemeral task management.
/// Supports dependency-based task ordering - tasks are only dequeued when all their dependencies are completed.
/// Uses a ready queue optimization to avoid scanning tasks that aren't ready.
/// </summary>
public class InMemoryTaskQueue : ITaskQueue
{
    private readonly ConcurrentQueue<ITask> _readyQueue = new();
    private readonly ConcurrentQueue<ITask> _waitingQueue = new();
    private readonly ConcurrentDictionary<string, ITask> _allTasks = new();
    private readonly ConcurrentDictionary<string, Status> _taskStatuses = new();
    private readonly object _queueLock = new();

    public Task EnqueueAsync<TInput, TOutput>(ITaskImplementation<TInput, TOutput> task, CancellationToken cancellationToken = default)
    {
        _allTasks[task.Id] = task;
        _taskStatuses[task.Id] = task.Status;
        
        // Set status to Queued
        task.SetStatus(Status.Queued);
        _taskStatuses[task.Id] = Status.Queued;
        
        // Add to appropriate queue based on dependencies
        lock (_queueLock)
        {
            if (AreDependenciesMet(task))
            {
                _readyQueue.Enqueue(task);
            }
            else
            {
                task.SetStatus(Status.WaitingOnDependencies);
                _taskStatuses[task.Id] = Status.WaitingOnDependencies;
                _waitingQueue.Enqueue(task);
            }
        }
        
        return Task.CompletedTask;
    }

    public Task<ITask?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        lock (_queueLock)
        {
            // Try to dequeue from ready queue
            if (_readyQueue.TryDequeue(out var task))
            {
                // Set status to SentToRunner
                task.SetStatus(Status.SentToRunner);
                _taskStatuses[task.Id] = Status.SentToRunner;
                
                return Task.FromResult<ITask?>(task);
            }
        }
        
        return Task.FromResult<ITask?>(null);
    }

    /// <summary>
    /// Gets information about a specific task by ID, including completed tasks.
    /// </summary>
    public ITask? GetTaskInfo(string taskId)
    {
        _allTasks.TryGetValue(taskId, out var task);
        return task;
    }

    /// <summary>
    /// Marks a task as completed. This allows dependent tasks to be dequeued.
    /// </summary>
    public void MarkTaskCompleted(string taskId, bool success = true)
    {
        var status = success ? Status.Completed : Status.Failed;
        _taskStatuses[taskId] = status;
        
        if (_allTasks.TryGetValue(taskId, out var task))
        {
            task.SetStatus(status);
        }
        
        // Move any waiting tasks that are now ready to the ready queue
        lock (_queueLock)
        {
            MoveReadyTasksFromWaitingQueue();
        }
    }

    private void MoveReadyTasksFromWaitingQueue()
    {
        var stillWaiting = new List<ITask>();
        
        while (_waitingQueue.TryDequeue(out var task))
        {
            if (AreDependenciesMet(task))
            {
                // Dependencies are now met, move to ready queue
                task.SetStatus(Status.Queued);
                _taskStatuses[task.Id] = Status.Queued;
                _readyQueue.Enqueue(task);
            }
            else
            {
                // Still waiting
                stillWaiting.Add(task);
            }
        }
        
        // Re-enqueue tasks still waiting
        foreach (var task in stillWaiting)
        {
            _waitingQueue.Enqueue(task);
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
