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
        await context.Queue.EnqueueSubTaskAsync(googleSearchTask, this.Id, cancellationToken);
        Console.WriteLine($"[SearchWebTask] Enqueued GoogleSearchTask (ID: {googleSearchTask.Id})");
        
        // Create sub-task 2: Filter Pages
        var filterTask = new FilterPagesTask(new List<string> { "page1.html", "page2.html", "page3.html" });
        await context.Queue.EnqueueSubTaskAsync(filterTask, this.Id, cancellationToken);
        Console.WriteLine($"[SearchWebTask] Enqueued FilterPagesTask (ID: {filterTask.Id})");
        
        // Create sub-task 3: Download Pages
        var downloadTask = new DownloadPagesTask(new List<string> { "page1.html", "page2.html" });
        await context.Queue.EnqueueSubTaskAsync(downloadTask, this.Id, cancellationToken);
        Console.WriteLine($"[SearchWebTask] Enqueued DownloadPagesTask (ID: {downloadTask.Id})");
        
        _results.Add($"Created {SubTaskIds.Count} sub-tasks");
        Console.WriteLine($"[SearchWebTask] Completed. Sub-tasks: {string.Join(", ", SubTaskIds)}");
        
        return _results;
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

}

// Example tasks demonstrating dependency chains
public class GeneratePlanTask : TaskBase<string, List<string>>
{
    private List<string> _plan = new();

    public GeneratePlanTask(string problem) : base(problem) { }

    public override async Task<List<string>> ExecuteAsync(ITaskExecutionContext context, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[GeneratePlanTask] Generating plan for: {Input}");
        
        // Generate a plan
        _plan = new List<string>
        {
            "Analyze problem",
            "Fix problem",
            "Write new tests",
            "Run tests and fix issues"
        };
        
        Console.WriteLine($"[GeneratePlanTask] Generated plan with {_plan.Count} steps");
        
        // Enqueue ExecutePlanTask as next task (will wait for this task to complete)
        var executePlanTask = new ExecutePlanTask(_plan);
        await context.Queue.EnqueueNextTaskAsync(executePlanTask, this.Id, cancellationToken);
        Console.WriteLine($"[GeneratePlanTask] Enqueued ExecutePlanTask as next task (ID: {executePlanTask.Id})");
        
        return _plan;
    }

}

public class ExecutePlanTask : TaskBase<List<string>, List<string>>
{
    private List<string> _results = new();

    public ExecutePlanTask(List<string> planSteps) : base(planSteps) { }

    public override async Task<List<string>> ExecuteAsync(ITaskExecutionContext context, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[ExecutePlanTask] Executing plan with {Input.Count} steps");
        
        // Create a sub-task for each step of the plan
        foreach (var step in Input)
        {
            var stepTask = new PlanStepTask(step);
            await context.Queue.EnqueueSubTaskAsync(stepTask, this.Id, cancellationToken);
            Console.WriteLine($"  [ExecutePlanTask] Enqueued sub-task: {step} (ID: {stepTask.Id})");
        }
        
        // Enqueue ReviewTask as next task (will wait for this task AND all sub-tasks to complete)
        var reviewTask = new ReviewTask(Input.Count);
        await context.Queue.EnqueueNextTaskAsync(reviewTask, this.Id, cancellationToken);
        Console.WriteLine($"[ExecutePlanTask] Enqueued ReviewTask as next task (ID: {reviewTask.Id})");
        Console.WriteLine($"  [ExecutePlanTask] ReviewTask has {reviewTask.DependencyTaskIds.Count} dependencies");
        
        _results.Add($"Created {SubTaskIds.Count} sub-tasks");
        return _results;
    }

}

public class PlanStepTask : TaskBase<string, bool>
{
    public PlanStepTask(string step) : base(step) { }

    public override Task<bool> ExecuteAsync(ITaskExecutionContext context, CancellationToken cancellationToken)
    {
        Console.WriteLine($"    [PlanStepTask] Executing: {Input}");
        return Task.FromResult(true);
    }

}

public class ReviewTask : TaskBase<int, bool>
{
    public ReviewTask(int numberOfSteps) : base(numberOfSteps) { }

    public override Task<bool> ExecuteAsync(ITaskExecutionContext context, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[ReviewTask] Reviewing results of {Input} completed steps");
        Console.WriteLine($"[ReviewTask] All dependencies met! Review complete.");
        return Task.FromResult(true);
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
        
        Console.WriteLine("\n---\n");
        
        // Example 3: Task dependency chain (GeneratePlan -> ExecutePlan -> Review)
        Console.WriteLine("Example 3: Task dependency chain");
        var queue2 = new InMemoryTaskQueue();
        
        // Enqueue GeneratePlanTask
        var generateTask = new GeneratePlanTask("Fix authentication bug");
        await queue2.EnqueueAsync(generateTask, CancellationToken.None);
        
        // Execute GeneratePlanTask - it will create ExecutePlanTask as next task
        await ExecuteNextTaskWithCompletion(queue2);
        
        // Execute all sub-tasks created by ExecutePlanTask
        Console.WriteLine("\n[Main] Executing sub-tasks...");
        int taskCount = 0;
        while (taskCount < 10) // Safety limit
        {
            var executed = await ExecuteNextTaskWithCompletion(queue2);
            if (!executed) break;
            taskCount++;
            await Task.Delay(150);
        }
        
        Console.WriteLine($"\n[Main] Dependency chain example completed!");
    }

    static async Task<bool> ExecuteNextTask(ITaskQueue queue)
    {
        var dequeued = await queue.DequeueAsync(CancellationToken.None);
        if (dequeued == null) return false;
        
        // Create execution context and execute using non-generic interface
        var context = new TaskExecutionContext(queue);
        
        if (dequeued is ITaskExecutable executable)
        {
            await executable.ExecuteAsync(context, CancellationToken.None);
        }
        
        return true;
    }

    static async Task<bool> ExecuteNextTaskWithCompletion(ITaskQueue queue)
    {
        var dequeued = await queue.DequeueAsync(CancellationToken.None);
        if (dequeued == null) return false;
        
        Console.WriteLine($"[Main] Dequeued task: {dequeued.GetType().Name} (ID: {dequeued.Id}, Status: {dequeued.Status}, Dependencies: {dequeued.DependencyTaskIds.Count})");
        
        // Create execution context and execute using non-generic interface
        var context = new TaskExecutionContext(queue);
        
        if (dequeued is ITaskExecutable executable)
        {
            await executable.ExecuteAsync(context, CancellationToken.None);
        }
        
        // Mark task as completed
        if (queue is InMemoryTaskQueue inMemoryQueue)
        {
            inMemoryQueue.MarkTaskCompleted(dequeued.Id, success: true);
            Console.WriteLine($"[Main] Marked task {dequeued.Id} as completed");
        }
        
        return true;
    }
}
