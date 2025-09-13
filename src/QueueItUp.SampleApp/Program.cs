using QueueItUp.Abstractions;
using QueueItUp.Core;
using QueueItUp.InMemory;

namespace QueueItUp.SampleApp;

public class PrintTask : TaskBase<string, bool>
{
    public PrintTask(string input) : base(input) { }
    public override Task<bool> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Executing PrintTask: {Input}");
        return Task.FromResult(true);
    }
}

class Program
{
    static async Task Main()
    {
        ITaskQueue queue = new InMemoryTaskQueue();
        var task = new PrintTask("Hello, QueueItUp!");
        await queue.EnqueueAsync(task);
        var dequeued = await queue.DequeueAsync();
        if (dequeued != null)
        {
            await dequeued.ExecuteAsync();
        }
    }
}
