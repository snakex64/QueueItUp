using QueueItUp.Abstractions;
using QueueItUp.Core;
using QueueItUp.InMemory;

namespace QueueItUp.Tests;

public class InMemoryTaskQueueTests
{
    private class SimpleTask : TaskBase<string, bool>
    {
        public SimpleTask(string input) : base(input) { }

        public override Task<bool> ExecuteAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public override Task<bool> LoadOutputAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }

    [Fact]
    public async Task EnqueueAsync_ShouldAddTaskToQueue()
    {
        // Arrange
        ITaskQueue queue = new InMemoryTaskQueue();
        var task = new SimpleTask("test");

        // Act
        await queue.EnqueueAsync(task, CancellationToken.None);
        var dequeued = await queue.DequeueAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(dequeued);
        Assert.Equal(task.Id, dequeued.Id);
    }

    [Fact]
    public async Task DequeueAsync_OnEmptyQueue_ShouldReturnNull()
    {
        // Arrange
        ITaskQueue queue = new InMemoryTaskQueue();

        // Act
        var dequeued = await queue.DequeueAsync(CancellationToken.None);

        // Assert
        Assert.Null(dequeued);
    }

    [Fact]
    public async Task Queue_ShouldMaintainFIFOOrder()
    {
        // Arrange
        ITaskQueue queue = new InMemoryTaskQueue();
        var task1 = new SimpleTask("first");
        var task2 = new SimpleTask("second");
        var task3 = new SimpleTask("third");

        // Act
        await queue.EnqueueAsync(task1, CancellationToken.None);
        await queue.EnqueueAsync(task2, CancellationToken.None);
        await queue.EnqueueAsync(task3, CancellationToken.None);

        var dequeued1 = await queue.DequeueAsync(CancellationToken.None);
        var dequeued2 = await queue.DequeueAsync(CancellationToken.None);
        var dequeued3 = await queue.DequeueAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(dequeued1);
        Assert.NotNull(dequeued2);
        Assert.NotNull(dequeued3);
        Assert.Equal(task1.Id, dequeued1.Id);
        Assert.Equal(task2.Id, dequeued2.Id);
        Assert.Equal(task3.Id, dequeued3.Id);
    }

    [Fact]
    public async Task Queue_ShouldHandleMultipleTaskTypes()
    {
        // Arrange
        ITaskQueue queue = new InMemoryTaskQueue();
        var stringTask = new SimpleTask("test");
        var numberTask = new NumberTask(42);

        // Act
        await queue.EnqueueAsync(stringTask, CancellationToken.None);
        await queue.EnqueueAsync(numberTask, CancellationToken.None);

        var dequeued1 = await queue.DequeueAsync(CancellationToken.None);
        var dequeued2 = await queue.DequeueAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(dequeued1);
        Assert.NotNull(dequeued2);
        Assert.IsAssignableFrom<ITaskImplementation<string, bool>>(dequeued1);
        Assert.IsAssignableFrom<ITaskImplementation<int, int>>(dequeued2);
    }

    [Fact]
    public async Task EnqueueAsync_ShouldSupportCancellationToken()
    {
        // Arrange
        ITaskQueue queue = new InMemoryTaskQueue();
        var task = new SimpleTask("test");
        var cts = new CancellationTokenSource();

        // Act - should not throw
        await queue.EnqueueAsync(task, cts.Token);

        // Assert
        var dequeued = await queue.DequeueAsync(CancellationToken.None);
        Assert.NotNull(dequeued);
    }

    [Fact]
    public async Task DequeueAsync_ShouldSupportCancellationToken()
    {
        // Arrange
        ITaskQueue queue = new InMemoryTaskQueue();
        var cts = new CancellationTokenSource();

        // Act - should not throw
        var result = await queue.DequeueAsync(cts.Token);

        // Assert
        Assert.Null(result);
    }

    private class NumberTask : TaskBase<int, int>
    {
        public NumberTask(int input) : base(input) { }

        public override Task<int> ExecuteAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Input * 2);
        }

        public override Task<int> LoadOutputAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(Input * 2);
        }
    }
}
