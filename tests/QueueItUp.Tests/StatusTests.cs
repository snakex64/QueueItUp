using QueueItUp.Abstractions;

namespace QueueItUp.Tests;

public class StatusTests
{
    [Fact]
    public void Status_ShouldHaveExpectedValues()
    {
        // Verify that all expected status values exist
        Assert.Equal(0, (int)Status.New);
        Assert.Equal(1, (int)Status.Queued);
        Assert.Equal(2, (int)Status.SentToRunner);
        Assert.Equal(3, (int)Status.WaitingOnDependencies);
        Assert.Equal(4, (int)Status.Running);
        Assert.Equal(5, (int)Status.Completed);
        Assert.Equal(6, (int)Status.Failed);
        Assert.Equal(7, (int)Status.Canceled);
    }

    [Fact]
    public void Status_ShouldHaveEightValues()
    {
        // Verify we have exactly 8 status values
        var statusValues = Enum.GetValues(typeof(Status));
        Assert.Equal(8, statusValues.Length);
    }
}
