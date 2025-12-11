using QueueItUp.Agent.Plugins;

namespace QueueItUp.Tests;

public class CodingAgentCompletionPluginTests
{
    [Fact]
    public void CodingAgentCompletionPlugin_InitialState_ShouldNotBeCompleted()
    {
        // Arrange & Act
        var plugin = new CodingAgentCompletionPlugin();

        // Assert
        Assert.False(plugin.IsCompleted);
        Assert.Null(plugin.CompletionDescription);
    }

    [Fact]
    public void CodingAgentCompletionPlugin_MarkComplete_ShouldSetCompletedState()
    {
        // Arrange
        var plugin = new CodingAgentCompletionPlugin();
        var description = "Successfully implemented the feature";

        // Act
        var result = plugin.MarkComplete(description);

        // Assert
        Assert.True(plugin.IsCompleted);
        Assert.Equal(description, plugin.CompletionDescription);
        Assert.Contains("Task marked as complete", result);
        Assert.Contains(description, result);
    }

    [Fact]
    public void CodingAgentCompletionPlugin_MarkComplete_Multiple_ShouldUpdateDescription()
    {
        // Arrange
        var plugin = new CodingAgentCompletionPlugin();
        var description1 = "First completion";
        var description2 = "Second completion";

        // Act
        plugin.MarkComplete(description1);
        plugin.MarkComplete(description2);

        // Assert
        Assert.True(plugin.IsCompleted);
        Assert.Equal(description2, plugin.CompletionDescription);
    }
}
