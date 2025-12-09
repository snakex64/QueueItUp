using QueueItUp.Abstractions;

namespace QueueItUp.Core;

/// <summary>
/// Abstract base class for tasks, providing typed input/output and async execution.
/// Core project contains implementations and helpers that build on the light-weight abstractions project.
/// </summary>
public abstract class TaskBase<TInput, TOutput> : ITaskImplementation<TInput, TOutput>, ITaskExecutable
{
    private readonly List<string> _subTaskIds = new();
    private readonly List<string> _dependencyTaskIds = new();
    private TOutput? _output;

    public string Id { get; protected set; } = Guid.NewGuid().ToString();
    public Status Status { get; protected set; } = Status.New;
    public string? ParentTaskId { get; private set; }
    public IReadOnlyList<string> SubTaskIds => _subTaskIds.AsReadOnly();
    public IReadOnlyList<string> DependencyTaskIds => _dependencyTaskIds.AsReadOnly();

    public TInput Input { get; protected set; }
    
    /// <summary>
    /// Gets the output of the task after execution. May be null if task hasn't executed yet.
    /// </summary>
    public TOutput? Output => _output;

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
    
    /// <summary>
    /// Sets the output value after task execution.
    /// </summary>
    protected void SetOutput(TOutput output)
    {
        _output = output;
    }

    public abstract Task<TOutput> ExecuteAsync(ITaskExecutionContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Non-generic ExecuteAsync implementation that calls the typed version.
    /// </summary>
    async Task ITaskExecutable.ExecuteAsync(ITaskExecutionContext context, CancellationToken cancellationToken)
    {
        var result = await ExecuteAsync(context, cancellationToken);
        SetOutput(result);
    }

    public virtual Task<TInput> LoadInputAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(Input);
    }

    public virtual Task<TOutput> LoadOutputAsync(CancellationToken cancellationToken)
    {
        if (_output == null)
        {
            throw new InvalidOperationException($"Task {Id} has not been executed yet or did not produce an output.");
        }
        return Task.FromResult(_output);
    }
}
