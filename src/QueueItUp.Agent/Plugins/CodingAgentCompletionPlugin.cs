using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace QueueItUp.Agent.Plugins;

/// <summary>
/// Plugin that allows the CodingAgent to signal completion and provide a summary.
/// </summary>
public class CodingAgentCompletionPlugin
{
    private bool _isCompleted = false;
    private string? _completionDescription;

    public bool IsCompleted => _isCompleted;
    public string? CompletionDescription => _completionDescription;

    /// <summary>
    /// Marks the coding task as complete with a description of what was done.
    /// </summary>
    [KernelFunction, Description("Call this when the coding task is complete. Provide a brief description of what was done.")]
    public string MarkComplete(
        [Description("A brief description of what was accomplished in this coding task")] string description)
    {
        _isCompleted = true;
        _completionDescription = description;
        return $"Task marked as complete. Description: {description}";
    }
}
