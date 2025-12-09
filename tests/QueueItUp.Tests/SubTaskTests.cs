using QueueItUp.Abstractions;
using QueueItUp.Core;
using QueueItUp.InMemory;

namespace QueueItUp.Tests;

public class SubTaskTests
{
    private class ParentTask : TaskBase<string, List<string>>
    {
        private List<string> _results = new();

        public ParentTask(string input) : base(input) { }

        public override async Task<List<string>> ExecuteAsync(ITaskExecutionContext context, CancellationToken cancellationToken)
        {
            // Create two sub-tasks
            var subTask1 = new ChildTask($"{Input}-sub1");
            var subTask2 = new ChildTask($"{Input}-sub2");

            await context.EnqueueSubTaskAsync(subTask1, cancellationToken);
            await context.EnqueueSubTaskAsync(subTask2, cancellationToken);

            _results.Add($"Created {SubTaskIds.Count} sub-tasks");
            return _results;
        }

        public override Task<List<string>> LoadOutputAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_results);
        }
    }

    private class ChildTask : TaskBase<string, bool>
    {
        public ChildTask(string input) : base(input) { }

        public override Task<bool> ExecuteAsync(ITaskExecutionContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public override Task<bool> LoadOutputAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }

    [Fact]
    public async Task ExecutionContext_EnqueueSubTask_ShouldSetParentTaskId()
    {
        // Arrange
        ITaskQueue queue = new InMemoryTaskQueue();
        var parentTask = new ParentTask("parent");
        var context = new TaskExecutionContext(parentTask, queue);

        // Act
        var childTask = new ChildTask("child");
        await context.EnqueueSubTaskAsync(childTask, CancellationToken.None);

        // Assert
        Assert.Equal(parentTask.Id, childTask.ParentTaskId);
    }

    [Fact]
    public async Task ExecutionContext_EnqueueSubTask_ShouldAddToParentSubTaskIds()
    {
        // Arrange
        ITaskQueue queue = new InMemoryTaskQueue();
        var parentTask = new ParentTask("parent");
        var context = new TaskExecutionContext(parentTask, queue);

        // Act
        var childTask = new ChildTask("child");
        await context.EnqueueSubTaskAsync(childTask, CancellationToken.None);

        // Assert
        Assert.Single(parentTask.SubTaskIds);
        Assert.Contains(childTask.Id, parentTask.SubTaskIds);
    }

    [Fact]
    public async Task ExecutionContext_EnqueueSubTask_ShouldEnqueueToQueue()
    {
        // Arrange
        ITaskQueue queue = new InMemoryTaskQueue();
        var parentTask = new ParentTask("parent");
        var context = new TaskExecutionContext(parentTask, queue);

        // Act
        var childTask = new ChildTask("child");
        await context.EnqueueSubTaskAsync(childTask, CancellationToken.None);
        var dequeued = await queue.DequeueAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(dequeued);
        Assert.Equal(childTask.Id, dequeued.Id);
    }

    [Fact]
    public async Task ParentTask_CanCreateMultipleSubTasks()
    {
        // Arrange
        ITaskQueue queue = new InMemoryTaskQueue();
        var parentTask = new ParentTask("parent");
        await queue.EnqueueAsync(parentTask, CancellationToken.None);

        // Act - Execute parent task
        var dequeued = await queue.DequeueAsync(CancellationToken.None);
        Assert.NotNull(dequeued);
        var context = new TaskExecutionContext(dequeued, queue);
        
        if (dequeued is ITaskExecutable executable)
        {
            await executable.ExecuteAsync(context, CancellationToken.None);
        }

        // Assert - Parent should have 2 sub-tasks
        Assert.Equal(2, parentTask.SubTaskIds.Count);

        // Assert - Queue should have 2 sub-tasks
        var subTask1 = await queue.DequeueAsync(CancellationToken.None);
        var subTask2 = await queue.DequeueAsync(CancellationToken.None);
        var subTask3 = await queue.DequeueAsync(CancellationToken.None);

        Assert.NotNull(subTask1);
        Assert.NotNull(subTask2);
        Assert.Null(subTask3); // Queue should be empty now

        Assert.Equal(parentTask.Id, subTask1.ParentTaskId);
        Assert.Equal(parentTask.Id, subTask2.ParentTaskId);
    }

    [Fact]
    public async Task SubTask_CanHaveItsOwnSubTasks()
    {
        // Arrange - Create a task hierarchy: Grandparent -> Parent -> Child
        ITaskQueue queue = new InMemoryTaskQueue();
        var grandparentTask = new ParentTask("grandparent");
        var context = new TaskExecutionContext(grandparentTask, queue);

        // Act - Grandparent creates parent as sub-task
        var parentTask = new ParentTask("parent");
        await context.EnqueueSubTaskAsync(parentTask, CancellationToken.None);

        // Parent creates its own sub-task
        var parentContext = new TaskExecutionContext(parentTask, queue);
        var childTask = new ChildTask("child");
        await parentContext.EnqueueSubTaskAsync(childTask, CancellationToken.None);

        // Assert
        Assert.Single(grandparentTask.SubTaskIds);
        Assert.Contains(parentTask.Id, grandparentTask.SubTaskIds);
        Assert.Equal(grandparentTask.Id, parentTask.ParentTaskId);

        Assert.Single(parentTask.SubTaskIds);
        Assert.Contains(childTask.Id, parentTask.SubTaskIds);
        Assert.Equal(parentTask.Id, childTask.ParentTaskId);
    }

    [Fact]
    public async Task ExecutionContext_ShouldExposeCurrentTaskId()
    {
        // Arrange
        ITaskQueue queue = new InMemoryTaskQueue();
        var task = new ParentTask("test");
        var context = new TaskExecutionContext(task, queue);

        // Assert
        Assert.Equal(task.Id, context.CurrentTaskId);
    }

    [Fact]
    public async Task ExecutionContext_ShouldExposeQueue()
    {
        // Arrange
        ITaskQueue queue = new InMemoryTaskQueue();
        var task = new ParentTask("test");
        var context = new TaskExecutionContext(task, queue);

        // Assert
        Assert.Same(queue, context.Queue);
    }
}
