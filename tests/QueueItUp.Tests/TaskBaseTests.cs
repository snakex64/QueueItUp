using QueueItUp.Abstractions;
using QueueItUp.Core;

namespace QueueItUp.Tests;

public class TaskBaseTests
{
    private class TestTask : TaskBase<string, int>
    {
        private int _executionCount = 0;
        private int? _output;

        public TestTask(string input) : base(input) { }

        public int ExecutionCount => _executionCount;

        public override Task<int> ExecuteAsync(ITaskExecutionContext context, CancellationToken cancellationToken)
        {
            _executionCount++;
            _output = Input.Length;
            return Task.FromResult(_output.Value);
        }

    }

    [Fact]
    public void TaskBase_Constructor_ShouldInitializeWithInput()
    {
        // Arrange & Act
        var task = new TestTask("test input");

        // Assert
        Assert.Equal("test input", task.Input);
        Assert.NotNull(task.Id);
        Assert.NotEmpty(task.Id);
        Assert.Equal(Status.New, task.Status);
        Assert.Null(task.ParentTaskId);
        Assert.Empty(task.SubTaskIds);
    }

    [Fact]
    public void TaskBase_Id_ShouldBeUnique()
    {
        // Arrange & Act
        var task1 = new TestTask("input1");
        var task2 = new TestTask("input2");

        // Assert
        Assert.NotEqual(task1.Id, task2.Id);
    }

    [Fact]
    public async Task TaskBase_ExecuteAsync_ShouldReturnExpectedOutput()
    {
        // Arrange
        var task = new TestTask("hello");
        var queue = new InMemory.InMemoryTaskQueue();
        var context = new TaskExecutionContext(task, queue);

        // Act
        var result = await task.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.Equal(5, result);
        Assert.Equal(1, task.ExecutionCount);
    }

    [Fact]
    public async Task TaskBase_LoadInputAsync_ShouldReturnInput()
    {
        // Arrange
        var expectedInput = "test input";
        var task = new TestTask(expectedInput);

        // Act
        var actualInput = await task.LoadInputAsync(CancellationToken.None);

        // Assert
        Assert.Equal(expectedInput, actualInput);
    }

    [Fact]
    public async Task TaskBase_LoadOutputAsync_ShouldReturnOutput()
    {
        // Arrange
        var task = new TestTask("test");
        var queue = new InMemory.InMemoryTaskQueue();
        var context = new TaskExecutionContext(task, queue);
        
        // Execute through ITaskExecutable to trigger SetOutput
        if (task is ITaskExecutable executable)
        {
            await executable.ExecuteAsync(context, CancellationToken.None);
        }

        // Act
        var output = await task.LoadOutputAsync(CancellationToken.None);

        // Assert
        Assert.Equal(4, output);
    }

    [Fact]
    public void TaskBase_ShouldImplementITaskImplementation()
    {
        // Arrange
        var task = new TestTask("test");

        // Assert
        Assert.IsAssignableFrom<ITaskImplementation<string, int>>(task);
    }

    [Fact]
    public void TaskBase_ShouldImplementITaskInfo()
    {
        // Arrange
        var task = new TestTask("test");

        // Assert
        Assert.IsAssignableFrom<ITaskInfo<string, int>>(task);
    }

    [Fact]
    public void TaskBase_ShouldImplementITask()
    {
        // Arrange
        var task = new TestTask("test");

        // Assert
        Assert.IsAssignableFrom<ITask>(task);
    }

    [Fact]
    public void TaskBase_SetParentTaskId_ShouldSetParent()
    {
        // Arrange
        var task = new TestTask("test");
        var parentId = "parent-123";

        // Act
        task.SetParentTaskId(parentId);

        // Assert
        Assert.Equal(parentId, task.ParentTaskId);
    }

    [Fact]
    public void TaskBase_AddSubTaskId_ShouldAddToCollection()
    {
        // Arrange
        var task = new TestTask("test");
        var subTaskId1 = "sub-1";
        var subTaskId2 = "sub-2";

        // Act
        task.AddSubTaskId(subTaskId1);
        task.AddSubTaskId(subTaskId2);

        // Assert
        Assert.Equal(2, task.SubTaskIds.Count);
        Assert.Contains(subTaskId1, task.SubTaskIds);
        Assert.Contains(subTaskId2, task.SubTaskIds);
    }
}
