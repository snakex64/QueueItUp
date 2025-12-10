using QueueItUp.Abstractions;
using System.Collections.Concurrent;

namespace QueueItUp.InMemory;

/// <summary>
/// In-memory implementation of ITaskQueue for fast, ephemeral task management.
/// Supports dependency-based task ordering - tasks are only dequeued when all their dependencies are completed.
/// Uses separate dictionaries per status for optimized lookups.
/// </summary>
public class InMemoryTaskQueue : ITaskQueue
{
    private readonly ConcurrentQueue<ITask> _readyQueue = new();
    private readonly ConcurrentQueue<ITask> _waitingQueue = new();
    
    // Dictionary per status for fast lookups
    private readonly ConcurrentDictionary<string, ITask> _queuedTasks = new();
    private readonly ConcurrentDictionary<string, ITask> _sentToRunnerTasks = new();
    private readonly ConcurrentDictionary<string, ITask> _waitingOnDependenciesTasks = new();
    private readonly ConcurrentDictionary<string, ITask> _runningTasks = new();
    private readonly ConcurrentDictionary<string, ITask> _completedTasks = new();
    private readonly ConcurrentDictionary<string, ITask> _failedTasks = new();
    private readonly ConcurrentDictionary<string, ITask> _canceledTasks = new();
    
    // For name-based lookups
    private readonly ConcurrentDictionary<string, string> _taskNameToId = new();
    
    private readonly SemaphoreSlim _queueSemaphore = new(1, 1);

    public async Task EnqueueAsync<TInput, TOutput>(ITaskImplementation<TInput, TOutput> task, CancellationToken cancellationToken = default)
    {
        await _queueSemaphore.WaitAsync(cancellationToken);
        try
        {
            // Register task by name
            _taskNameToId[task.Name] = task.Id;
            
            // Set status to Queued
            task.SetStatus(Status.Queued);
            
            // Add to appropriate queue based on dependencies
            if (AreDependenciesMet(task))
            {
                _readyQueue.Enqueue(task);
                _queuedTasks[task.Id] = task;
            }
            else
            {
                task.SetStatus(Status.WaitingOnDependencies);
                _waitingQueue.Enqueue(task);
                _waitingOnDependenciesTasks[task.Id] = task;
            }
        }
        finally
        {
            _queueSemaphore.Release();
        }
    }

    public async Task EnqueueSubTaskAsync<TInput, TOutput>(ITaskImplementation<TInput, TOutput> subTask, string parentTaskId, CancellationToken cancellationToken = default)
    {
        // Set the parent-child relationship on the sub-task
        subTask.SetParentTaskId(parentTaskId);
        
        // Register the sub-task with the parent
        if (TryGetTaskInfo(parentTaskId, out var parentTask) && parentTask != null)
        {
            parentTask.AddSubTaskId(subTask.Id);
        }
        // Note: If parent not found, still enqueue the sub-task but parent-child link won't be bidirectional
        
        // Enqueue the sub-task
        await EnqueueAsync(subTask, cancellationToken);
    }

    public async Task EnqueueNextTaskAsync<TInput, TOutput>(ITaskImplementation<TInput, TOutput> nextTask, string afterTaskId, CancellationToken cancellationToken = default)
    {
        // The next task depends on the specified task
        nextTask.AddDependencyTaskId(afterTaskId);
        
        // Enqueue the next task
        await EnqueueAsync(nextTask, cancellationToken);
    }

    public async Task<ITask?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        await _queueSemaphore.WaitAsync(cancellationToken);
        try
        {
            // Try to dequeue from ready queue
            if (_readyQueue.TryDequeue(out var task))
            {
                // Move from queued to sent to runner
                _queuedTasks.TryRemove(task.Id, out _);
                task.SetStatus(Status.SentToRunner);
                _sentToRunnerTasks[task.Id] = task;
                
                return task;
            }
        }
        finally
        {
            _queueSemaphore.Release();
        }
        
        return null;
    }

    public bool TryGetTaskInfo(string taskId, out ITask? task)
    {
        // Check all status dictionaries
        if (_queuedTasks.TryGetValue(taskId, out task)) return true;
        if (_sentToRunnerTasks.TryGetValue(taskId, out task)) return true;
        if (_waitingOnDependenciesTasks.TryGetValue(taskId, out task)) return true;
        if (_runningTasks.TryGetValue(taskId, out task)) return true;
        if (_completedTasks.TryGetValue(taskId, out task)) return true;
        if (_failedTasks.TryGetValue(taskId, out task)) return true;
        if (_canceledTasks.TryGetValue(taskId, out task)) return true;
        
        task = null;
        return false;
    }

    public bool TryGetTaskInfoByName(string taskName, out ITask? task)
    {
        if (_taskNameToId.TryGetValue(taskName, out var taskId))
        {
            return TryGetTaskInfo(taskId, out task);
        }
        
        task = null;
        return false;
    }

    public async void MarkTaskCompleted(string taskId, bool success = true)
    {
        await _queueSemaphore.WaitAsync();
        try
        {
            var status = success ? Status.Completed : Status.Failed;
            
            if (TryGetTaskInfo(taskId, out var task) && task != null)
            {
                // Remove from previous status dictionary
                RemoveFromAllDictionaries(taskId);
                
                // Update status and add to appropriate dictionary
                task.SetStatus(status);
                if (success)
                {
                    _completedTasks[taskId] = task;
                }
                else
                {
                    _failedTasks[taskId] = task;
                }
            }
            
            // Move any waiting tasks that are now ready to the ready queue
            MoveReadyTasksFromWaitingQueue();
        }
        finally
        {
            _queueSemaphore.Release();
        }
    }

    private void RemoveFromAllDictionaries(string taskId)
    {
        _queuedTasks.TryRemove(taskId, out _);
        _sentToRunnerTasks.TryRemove(taskId, out _);
        _waitingOnDependenciesTasks.TryRemove(taskId, out _);
        _runningTasks.TryRemove(taskId, out _);
        _completedTasks.TryRemove(taskId, out _);
        _failedTasks.TryRemove(taskId, out _);
        _canceledTasks.TryRemove(taskId, out _);
    }

    private void MoveReadyTasksFromWaitingQueue()
    {
        var stillWaiting = new List<ITask>();
        
        while (_waitingQueue.TryDequeue(out var task))
        {
            if (AreDependenciesMet(task))
            {
                // Dependencies are now met, move to ready queue
                _waitingOnDependenciesTasks.TryRemove(task.Id, out _);
                task.SetStatus(Status.Queued);
                _queuedTasks[task.Id] = task;
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
        
        // Check if all dependencies are completed - use the completed dictionary for fast lookup
        foreach (var dependencyId in task.DependencyTaskIds)
        {
            if (!_completedTasks.ContainsKey(dependencyId))
            {
                return false;
            }
        }
        
        return true;
    }
}
