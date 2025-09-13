using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace QueueItUp.Abstractions;

/// <summary>
/// Represents a generic asynchronous task with typed input and output, supporting subtask management.
/// </summary>
/// <typeparam name="TInput">Type of the input parameter.</typeparam>
/// <typeparam name="TOutput">Type of the output/result.</typeparam>
public interface ITask<TInput, TOutput>
{
    TInput Input { get; }
    Task<TOutput> ExecuteAsync(CancellationToken cancellationToken = default);
    IReadOnlyList<ITask<object, object>> SubTasks { get; }
    void AddSubTask<TSubInput, TSubOutput>(ITask<TSubInput, TSubOutput> subTask);
    Task AnalyzeSubTaskResultsAsync(CancellationToken cancellationToken = default);
}
