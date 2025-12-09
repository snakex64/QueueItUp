using QueueItUp.Abstractions;
using QueueItUp.Core;
using QueueItUp.InMemory;

namespace QueueItUp.Tests;

public class TaskInfoTests
{
    private class CalculateTask : TaskBase<int, int>
    {
        public CalculateTask(int input) : base(input) { }

        public override Task<int> ExecuteAsync(ITaskExecutionContext context, CancellationToken cancellationToken)
        {
            return Task.FromResult(Input * 2);
        }
    }

    [Fact]
    public async Task GetTaskInfo_ShouldReturnTaskInfo_ForEnqueuedTask()
    {
        // Arrange
        var queue = new InMemoryTaskQueue();
        var task = new CalculateTask(5);

        // Act
        await queue.EnqueueAsync(task, CancellationToken.None);
        var taskInfo = queue.GetTaskInfo(task.Id);

        // Assert
        Assert.NotNull(taskInfo);
        Assert.Equal(task.Id, taskInfo.Id);
        Assert.Equal(Status.Queued, taskInfo.Status);
    }

    [Fact]
    public void GetTaskInfo_ShouldReturnNull_ForNonExistentTask()
    {
        // Arrange
        var queue = new InMemoryTaskQueue();

        // Act
        var taskInfo = queue.GetTaskInfo("non-existent-id");

        // Assert
        Assert.Null(taskInfo);
    }

    [Fact]
    public async Task GetTaskInfo_ShouldReturnCompletedTask_WithOutputAccessible()
    {
        // Arrange
        var queue = new InMemoryTaskQueue();
        var task = new CalculateTask(5);
        await queue.EnqueueAsync(task, CancellationToken.None);

        // Execute the task
        var dequeued = await queue.DequeueAsync(CancellationToken.None);
        Assert.NotNull(dequeued);
        
        var context = new TaskExecutionContext(dequeued, queue);
        if (dequeued is ITaskExecutable executable)
        {
            await executable.ExecuteAsync(context, CancellationToken.None);
        }
        queue.MarkTaskCompleted(dequeued.Id);

        // Act - Get task info after completion
        var taskInfo = queue.GetTaskInfo(task.Id);

        // Assert
        Assert.NotNull(taskInfo);
        Assert.Equal(Status.Completed, taskInfo.Status);
        
        // Verify we can access the input and output through the task
        Assert.IsType<CalculateTask>(taskInfo);
        var calcTask = (CalculateTask)taskInfo;
        Assert.Equal(5, calcTask.Input);
        Assert.Equal(10, calcTask.Output);
    }

    [Fact]
    public async Task TaskOutput_ShouldBeAccessible_AfterExecution()
    {
        // Arrange
        var task = new CalculateTask(7);
        var queue = new InMemoryTaskQueue();
        var context = new TaskExecutionContext(task, queue);

        // Act - Execute through ITaskExecutable
        if (task is ITaskExecutable executable)
        {
            await executable.ExecuteAsync(context, CancellationToken.None);
        }

        // Assert
        Assert.Equal(14, task.Output);
    }

    [Fact]
    public async Task ChainedTasks_CanAccessPredecessorOutput()
    {
        // Arrange
        var queue = new InMemoryTaskQueue();
        var task1 = new CalculateTask(3);
        await queue.EnqueueAsync(task1, CancellationToken.None);

        // Execute task1
        var dequeued1 = await queue.DequeueAsync(CancellationToken.None);
        var context1 = new TaskExecutionContext(dequeued1!, queue);
        if (dequeued1 is ITaskExecutable executable1)
        {
            await executable1.ExecuteAsync(context1, CancellationToken.None);
        }
        queue.MarkTaskCompleted(dequeued1!.Id);

        // Act - Get task1 info and access its output for task2
        var task1Info = queue.GetTaskInfo(task1.Id);
        Assert.NotNull(task1Info);
        Assert.IsType<CalculateTask>(task1Info);
        var completedTask1 = (CalculateTask)task1Info;
        
        // Create task2 using task1's output
        var task2 = new CalculateTask(completedTask1.Output!);
        await queue.EnqueueAsync(task2, CancellationToken.None);

        // Execute task2
        var dequeued2 = await queue.DequeueAsync(CancellationToken.None);
        var context2 = new TaskExecutionContext(dequeued2!, queue);
        if (dequeued2 is ITaskExecutable executable2)
        {
            await executable2.ExecuteAsync(context2, CancellationToken.None);
        }

        // Assert
        Assert.Equal(6, completedTask1.Output); // 3 * 2
        Assert.Equal(12, task2.Output); // 6 * 2
    }
}
