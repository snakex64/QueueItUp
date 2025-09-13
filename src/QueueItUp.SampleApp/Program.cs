using QueueItUp.Abstractions;
using QueueItUp.Core;
using QueueItUp.InMemory;

namespace QueueItUp.SampleApp;

public class PrintTask : TaskBase<string, bool>
{
    public PrintTask(string input) : base(input) { }
    public override Task<bool> ExecuteAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine($"Executing PrintTask: {Input}");
        return Task.FromResult(true);
    }

    public override Task<bool> LoadOutputAsync(CancellationToken cancellationToken)
    {
        // For demonstration purposes, this task does not store an output beyond execution.
        return Task.FromResult(true);
    }
}

class Program
{
    static async Task Main()
    {
        var queue = new InMemoryTaskQueue();
        var task = new PrintTask("Hello, QueueItUp!");
        await queue.EnqueueAsync(task, CancellationToken.None);
        var dequeued = await queue.DequeueAsync(CancellationToken.None);
        if (dequeued is ITaskImplementation<string, bool> executable)
        {
            await executable.ExecuteAsync(CancellationToken.None);
        }
    }
}
