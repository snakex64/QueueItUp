using QueueItUp.Abstractions;
using QueueItUp.Core;
using QueueItUp.InMemory;

namespace QueueItUp.Tests;

public class DependencyTests
{
    private class SimpleTask : TaskBase<string, bool>
    {
        public SimpleTask(string input) : base(input) { }

        public override Task<bool> ExecuteAsync(ITaskExecutionContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public override Task<bool> LoadOutputAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }

    private class TaskWithNextTask : TaskBase<string, bool>
    {
        private readonly string _nextTaskInput;

        public TaskWithNextTask(string input, string nextTaskInput) : base(input)
        {
            _nextTaskInput = nextTaskInput;
        }

        public override async Task<bool> ExecuteAsync(ITaskExecutionContext context, CancellationToken cancellationToken)
        {
            // Enqueue a next task
            var nextTask = new SimpleTask(_nextTaskInput);
            await context.EnqueueNextTaskAsync(nextTask, cancellationToken);
            return true;
        }

        public override Task<bool> LoadOutputAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }

    private class TaskWithSubTasksAndNextTask : TaskBase<string, bool>
    {
        public TaskWithSubTasksAndNextTask(string input) : base(input) { }

        public override async Task<bool> ExecuteAsync(ITaskExecutionContext context, CancellationToken cancellationToken)
        {
            // Create 2 sub-tasks
            await context.EnqueueSubTaskAsync(new SimpleTask("sub1"), cancellationToken);
            await context.EnqueueSubTaskAsync(new SimpleTask("sub2"), cancellationToken);
            
            // Create a next task that depends on this task and all sub-tasks
            await context.EnqueueNextTaskAsync(new SimpleTask("next"), cancellationToken);
            
            return true;
        }

        public override Task<bool> LoadOutputAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }

    [Fact]
    public void Task_DependencyTaskIds_ShouldBeEmpty_ByDefault()
    {
        // Arrange & Act
        var task = new SimpleTask("test");

        // Assert
        Assert.Empty(task.DependencyTaskIds);
    }

    [Fact]
    public void Task_AddDependencyTaskId_ShouldAddToDependencyList()
    {
        // Arrange
        var task = new SimpleTask("test");
        var depId = "dependency-123";

        // Act
        task.AddDependencyTaskId(depId);

        // Assert
        Assert.Single(task.DependencyTaskIds);
        Assert.Contains(depId, task.DependencyTaskIds);
    }

    [Fact]
    public async Task EnqueueNextTaskAsync_ShouldSetDependencyOnCurrentTask()
    {
        // Arrange
        ITaskQueue queue = new InMemoryTaskQueue();
        var currentTask = new SimpleTask("current");
        var context = new TaskExecutionContext(currentTask, queue);
        var nextTask = new SimpleTask("next");

        // Act
        await context.EnqueueNextTaskAsync(nextTask, CancellationToken.None);

        // Assert
        Assert.Single(nextTask.DependencyTaskIds);
        Assert.Contains(currentTask.Id, nextTask.DependencyTaskIds);
    }

    [Fact]
    public async Task EnqueueNextTaskAsync_ShouldSetDependenciesOnAllSubTasks()
    {
        // Arrange
        ITaskQueue queue = new InMemoryTaskQueue();
        var currentTask = new SimpleTask("current");
        
        // Add some sub-tasks to current task
        currentTask.AddSubTaskId("sub-1");
        currentTask.AddSubTaskId("sub-2");
        
        var context = new TaskExecutionContext(currentTask, queue);
        var nextTask = new SimpleTask("next");

        // Act
        await context.EnqueueNextTaskAsync(nextTask, CancellationToken.None);

        // Assert
        Assert.Equal(3, nextTask.DependencyTaskIds.Count); // current + 2 sub-tasks
        Assert.Contains(currentTask.Id, nextTask.DependencyTaskIds);
        Assert.Contains("sub-1", nextTask.DependencyTaskIds);
        Assert.Contains("sub-2", nextTask.DependencyTaskIds);
    }

    [Fact]
    public async Task DequeueAsync_ShouldNotReturnTask_WhenDependenciesNotMet()
    {
        // Arrange
        var queue = new InMemoryTaskQueue();
        var task1 = new SimpleTask("task1");
        var task2 = new SimpleTask("task2");
        
        // task2 depends on task1
        task2.AddDependencyTaskId(task1.Id);
        
        await queue.EnqueueAsync(task1, CancellationToken.None);
        await queue.EnqueueAsync(task2, CancellationToken.None);

        // Act - First dequeue should return task1
        var dequeued1 = await queue.DequeueAsync(CancellationToken.None);
        
        // Act - Second dequeue should return null (task2's dependency not met)
        var dequeued2 = await queue.DequeueAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(dequeued1);
        Assert.Equal(task1.Id, dequeued1.Id);
        Assert.Null(dequeued2);
    }

    [Fact]
    public async Task DequeueAsync_ShouldReturnTask_AfterDependenciesCompleted()
    {
        // Arrange
        var queue = new InMemoryTaskQueue();
        var task1 = new SimpleTask("task1");
        var task2 = new SimpleTask("task2");
        
        // task2 depends on task1
        task2.AddDependencyTaskId(task1.Id);
        
        await queue.EnqueueAsync(task1, CancellationToken.None);
        await queue.EnqueueAsync(task2, CancellationToken.None);

        // Act - Dequeue and complete task1
        var dequeued1 = await queue.DequeueAsync(CancellationToken.None);
        queue.MarkTaskCompleted(dequeued1!.Id);
        
        // Act - Now task2 should be available
        var dequeued2 = await queue.DequeueAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(dequeued2);
        Assert.Equal(task2.Id, dequeued2.Id);
    }

    [Fact]
    public async Task DependencyChain_ShouldExecuteInCorrectOrder()
    {
        // Arrange
        var queue = new InMemoryTaskQueue();
        var executionOrder = new List<string>();
        
        var task1 = new SimpleTask("task1");
        var task2 = new SimpleTask("task2");
        var task3 = new SimpleTask("task3");
        
        // Chain: task1 -> task2 -> task3
        task2.AddDependencyTaskId(task1.Id);
        task3.AddDependencyTaskId(task2.Id);
        
        await queue.EnqueueAsync(task1, CancellationToken.None);
        await queue.EnqueueAsync(task2, CancellationToken.None);
        await queue.EnqueueAsync(task3, CancellationToken.None);

        // Act & Assert - Execute tasks in order
        var dequeued1 = await queue.DequeueAsync(CancellationToken.None);
        Assert.NotNull(dequeued1);
        Assert.Equal(task1.Id, dequeued1.Id);
        executionOrder.Add("task1");
        queue.MarkTaskCompleted(dequeued1.Id);
        
        var dequeued2 = await queue.DequeueAsync(CancellationToken.None);
        Assert.NotNull(dequeued2);
        Assert.Equal(task2.Id, dequeued2.Id);
        executionOrder.Add("task2");
        queue.MarkTaskCompleted(dequeued2.Id);
        
        var dequeued3 = await queue.DequeueAsync(CancellationToken.None);
        Assert.NotNull(dequeued3);
        Assert.Equal(task3.Id, dequeued3.Id);
        executionOrder.Add("task3");
        
        // Verify order
        Assert.Equal(new[] { "task1", "task2", "task3" }, executionOrder);
    }

    [Fact]
    public async Task TaskWithSubTasks_NextTaskShouldWaitForAllSubTasks()
    {
        // Arrange
        var queue = new InMemoryTaskQueue();
        var parentTask = new TaskWithSubTasksAndNextTask("parent");
        
        await queue.EnqueueAsync(parentTask, CancellationToken.None);
        
        // Execute parent task
        var dequeued = await queue.DequeueAsync(CancellationToken.None);
        var context = new TaskExecutionContext(dequeued!, queue);
        if (dequeued is ITaskExecutable executable)
        {
            await executable.ExecuteAsync(context, CancellationToken.None);
        }
        queue.MarkTaskCompleted(dequeued!.Id);

        // Act - Execute sub-tasks
        var sub1 = await queue.DequeueAsync(CancellationToken.None);
        Assert.NotNull(sub1);
        queue.MarkTaskCompleted(sub1.Id);
        
        // Next task should still not be available (one sub-task remaining)
        var nextAttempt1 = await queue.DequeueAsync(CancellationToken.None);
        Assert.NotNull(nextAttempt1); // Should get sub2
        Assert.IsType<SimpleTask>(nextAttempt1); // Verify it's a SimpleTask
        var nextAttempt1AsSimple = (SimpleTask)nextAttempt1;
        Assert.NotEqual("next", nextAttempt1AsSimple.Input); // Verify it's not the next task
        queue.MarkTaskCompleted(nextAttempt1.Id);
        
        // Now next task should be available
        var nextTask = await queue.DequeueAsync(CancellationToken.None);

        // Assert
        Assert.NotNull(nextTask);
        Assert.IsType<SimpleTask>(nextTask);
        var simpleNextTask = (SimpleTask)nextTask;
        Assert.Equal("next", simpleNextTask.Input);
    }

    [Fact]
    public async Task Task_SetStatus_ShouldUpdateStatus()
    {
        // Arrange
        var task = new SimpleTask("test");
        Assert.Equal(Status.New, task.Status);

        // Act
        task.SetStatus(Status.Running);

        // Assert
        Assert.Equal(Status.Running, task.Status);
    }

    [Fact]
    public async Task DequeueAsync_ShouldSetStatusToWaitingOnDependencies()
    {
        // Arrange
        var queue = new InMemoryTaskQueue();
        var task1 = new SimpleTask("task1");
        var task2 = new SimpleTask("task2");
        task2.AddDependencyTaskId(task1.Id);
        
        await queue.EnqueueAsync(task2, CancellationToken.None);

        // Act
        var dequeued = await queue.DequeueAsync(CancellationToken.None);

        // Assert
        Assert.Null(dequeued);
        Assert.Equal(Status.WaitingOnDependencies, task2.Status);
    }
}
