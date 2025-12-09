using QueueItUp.Abstractions;

namespace QueueItUp.Core;

/// <summary>
/// Abstract base class for tasks, providing typed input/output and async execution.
/// Core project contains implementations and helpers that build on the light-weight abstractions project.
/// </summary>
public abstract class TaskBase<TInput, TOutput> : ITaskImplementation<TInput, TOutput>, ITaskWithSubTasks, ITaskExecutable
{
    private readonly List<string> _subTaskIds = new();
    private readonly List<string> _dependencyTaskIds = new();

    public string Id { get; protected set; } = Guid.NewGuid().ToString();
    public Status Status { get; protected set; } = Status.New;
    public string? ParentTaskId { get; private set; }
    public IReadOnlyList<string> SubTaskIds => _subTaskIds.AsReadOnly();
    public IReadOnlyList<string> DependencyTaskIds => _dependencyTaskIds.AsReadOnly();

    public TInput Input { get; protected set; }

    protected TaskBase()
    {
        Input = default!;
    }

    protected TaskBase(TInput input)
    {
        Input = input;
    }

    /// <summary>
    /// Sets the parent task ID. This is typically called by the queue when enqueueing a sub-task.
    /// </summary>
    public void SetParentTaskId(string parentTaskId)
    {
        ParentTaskId = parentTaskId;
    }

    /// <summary>
    /// Registers a sub-task ID. This is typically called by the execution context when a sub-task is enqueued.
    /// </summary>
    public void AddSubTaskId(string subTaskId)
    {
        _subTaskIds.Add(subTaskId);
    }

    /// <summary>
    /// Adds a dependency task ID that must complete before this task can execute.
    /// </summary>
    public void AddDependencyTaskId(string dependencyTaskId)
    {
        _dependencyTaskIds.Add(dependencyTaskId);
    }

    /// <summary>
    /// Updates the task status.
    /// </summary>
    public void SetStatus(Status status)
    {
        Status = status;
    }

    public abstract Task<TOutput> ExecuteAsync(ITaskExecutionContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Non-generic ExecuteAsync implementation that calls the typed version.
    /// </summary>
    async Task ITaskExecutable.ExecuteAsync(ITaskExecutionContext context, CancellationToken cancellationToken)
    {
        await ExecuteAsync(context, cancellationToken);
    }

    public virtual Task<TInput> LoadInputAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(Input);
    }

    public abstract Task<TOutput> LoadOutputAsync(CancellationToken cancellationToken);
}
