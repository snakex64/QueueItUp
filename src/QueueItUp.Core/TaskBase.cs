using QueueItUp.Abstractions;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QueueItUp.Core;

/// <summary>
/// Abstract base class for tasks, providing subtask management and typed input/output.
/// Core project contains implementations and helpers that build on the light-weight abstractions project.
/// </summary>
public abstract class TaskBase<TInput, TOutput> : ITask<TInput, TOutput>
{
    public TInput Input { get; }
    private readonly List<ITask<object, object>> _subTasks = new();
    public IReadOnlyList<ITask<object, object>> SubTasks => _subTasks;

    protected TaskBase(TInput input)
    {
        Input = input;
    }

    public void AddSubTask<TSubInput, TSubOutput>(ITask<TSubInput, TSubOutput> subTask)
    {
        _subTasks.Add((ITask<object, object>)subTask);
    }

    public abstract Task<TOutput> ExecuteAsync(CancellationToken cancellationToken = default);

    public virtual Task AnalyzeSubTaskResultsAsync(CancellationToken cancellationToken = default)
    {
        // Default: do nothing. Override to analyze subtask results.
        return Task.CompletedTask;
    }
}
