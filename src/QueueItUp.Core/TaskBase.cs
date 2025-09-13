using QueueItUp.Abstractions;

namespace QueueItUp.Core;

/// <summary>
/// Abstract base class for tasks, providing typed input/output and async execution.
/// Core project contains implementations and helpers that build on the light-weight abstractions project.
/// </summary>
public abstract class TaskBase<TInput, TOutput> : ITaskImplementation<TInput, TOutput>
{
    public string Id { get; protected set; } = Guid.NewGuid().ToString();
    public Status Status { get; protected set; } = Status.New;

    public TInput Input { get; protected set; }

    protected TaskBase()
    {
        Input = default!;
    }

    protected TaskBase(TInput input)
    {
        Input = input;
    }

    public abstract Task<TOutput> ExecuteAsync(CancellationToken cancellationToken);

    public virtual Task<TInput> LoadInputAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(Input);
    }

    public abstract Task<TOutput> LoadOutputAsync(CancellationToken cancellationToken);
}
