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

        public override Task<int> ExecuteAsync(CancellationToken cancellationToken)
        {
            _executionCount++;
            _output = Input.Length;
            return Task.FromResult(_output.Value);
        }

        public override Task<int> LoadOutputAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_output ?? 0);
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

        // Act
        var result = await task.ExecuteAsync(CancellationToken.None);

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
        await task.ExecuteAsync(CancellationToken.None);

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
}
