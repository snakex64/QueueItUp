using QueueItUp.Abstractions;
using QueueItUp.Core;
using QueueItUp.InMemory;

namespace QueueItUp.SampleApp;

public class PrintTask : TaskBase<string, bool>
{
    public PrintTask(string input) : base(input) { }
    public override Task<bool> ExecuteAsync(ITaskExecutionContext context, CancellationToken cancellationToken)
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

// Example task that demonstrates sub-task creation
public class SearchWebTask : TaskBase<string, List<string>>
{
    private List<string> _results = new();

    public SearchWebTask(string searchQuery) : base(searchQuery) { }

    public override async Task<List<string>> ExecuteAsync(ITaskExecutionContext context, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[SearchWebTask] Starting search for: {Input}");
        
        // Create sub-task 1: Search Google
        var googleSearchTask = new GoogleSearchTask(Input);
        await context.EnqueueSubTaskAsync(googleSearchTask, cancellationToken);
        Console.WriteLine($"[SearchWebTask] Enqueued GoogleSearchTask (ID: {googleSearchTask.Id})");
        
        // Create sub-task 2: Filter Pages
        var filterTask = new FilterPagesTask(new List<string> { "page1.html", "page2.html", "page3.html" });
        await context.EnqueueSubTaskAsync(filterTask, cancellationToken);
        Console.WriteLine($"[SearchWebTask] Enqueued FilterPagesTask (ID: {filterTask.Id})");
        
        // Create sub-task 3: Download Pages
        var downloadTask = new DownloadPagesTask(new List<string> { "page1.html", "page2.html" });
        await context.EnqueueSubTaskAsync(downloadTask, cancellationToken);
        Console.WriteLine($"[SearchWebTask] Enqueued DownloadPagesTask (ID: {downloadTask.Id})");
        
        _results.Add($"Created {SubTaskIds.Count} sub-tasks");
        Console.WriteLine($"[SearchWebTask] Completed. Sub-tasks: {string.Join(", ", SubTaskIds)}");
        
        return _results;
    }

    public override Task<List<string>> LoadOutputAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(_results);
    }
}

public class GoogleSearchTask : TaskBase<string, List<string>>
{
    private List<string> _searchResults = new();

    public GoogleSearchTask(string query) : base(query) { }

    public override Task<List<string>> ExecuteAsync(ITaskExecutionContext context, CancellationToken cancellationToken)
    {
        Console.WriteLine($"  [GoogleSearchTask] Searching Google for: {Input}");
        _searchResults = new List<string> { "result1.html", "result2.html", "result3.html" };
        Console.WriteLine($"  [GoogleSearchTask] Found {_searchResults.Count} results");
        return Task.FromResult(_searchResults);
    }

    public override Task<List<string>> LoadOutputAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(_searchResults);
    }
}

public class FilterPagesTask : TaskBase<List<string>, List<string>>
{
    private List<string> _filteredPages = new();

    public FilterPagesTask(List<string> pages) : base(pages) { }

    public override Task<List<string>> ExecuteAsync(ITaskExecutionContext context, CancellationToken cancellationToken)
    {
        Console.WriteLine($"  [FilterPagesTask] Filtering {Input.Count} pages");
        _filteredPages = Input.Where(p => p.Contains("1") || p.Contains("2")).ToList();
        Console.WriteLine($"  [FilterPagesTask] Kept {_filteredPages.Count} pages after filtering");
        return Task.FromResult(_filteredPages);
    }

    public override Task<List<string>> LoadOutputAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(_filteredPages);
    }
}

public class DownloadPagesTask : TaskBase<List<string>, int>
{
    private int _downloadedCount = 0;

    public DownloadPagesTask(List<string> pages) : base(pages) { }

    public override Task<int> ExecuteAsync(ITaskExecutionContext context, CancellationToken cancellationToken)
    {
        Console.WriteLine($"  [DownloadPagesTask] Downloading {Input.Count} pages");
        _downloadedCount = Input.Count;
        Console.WriteLine($"  [DownloadPagesTask] Successfully downloaded {_downloadedCount} pages");
        return Task.FromResult(_downloadedCount);
    }

    public override Task<int> LoadOutputAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(_downloadedCount);
    }
}

class Program
{
    static async Task Main()
    {
        Console.WriteLine("=== QueueItUp Sample App ===\n");
        
        // Example 1: Simple task without sub-tasks
        Console.WriteLine("Example 1: Simple task");
        ITaskQueue queue = new InMemoryTaskQueue();
        var simpleTask = new PrintTask("Hello, QueueItUp!");
        await queue.EnqueueAsync(simpleTask, CancellationToken.None);
        await ExecuteNextTask(queue);
        
        Console.WriteLine("\n---\n");
        
        // Example 2: Task with sub-tasks
        Console.WriteLine("Example 2: Task with sub-tasks");
        var searchTask = new SearchWebTask("AI programming");
        await queue.EnqueueAsync(searchTask, CancellationToken.None);
        
        // Execute all tasks in the queue
        while (true)
        {
            var executed = await ExecuteNextTask(queue);
            if (!executed) break;
            await Task.Delay(100); // Small delay for readability
        }
        
        Console.WriteLine($"\n[Main] All tasks completed!");
        Console.WriteLine($"[Main] SearchWebTask had {searchTask.SubTaskIds.Count} sub-tasks");
    }

    static async Task<bool> ExecuteNextTask(ITaskQueue queue)
    {
        var dequeued = await queue.DequeueAsync(CancellationToken.None);
        if (dequeued == null) return false;
        
        // Create execution context and execute
        var context = new TaskExecutionContext(dequeued, queue);
        
        // Use reflection to call ExecuteAsync since we don't know the generic types at compile time
        var executeMethod = dequeued.GetType().GetMethod("ExecuteAsync");
        if (executeMethod != null)
        {
            var task = executeMethod.Invoke(dequeued, new object[] { context, CancellationToken.None }) as Task;
            if (task != null)
            {
                await task;
            }
        }
        
        return true;
    }
}
