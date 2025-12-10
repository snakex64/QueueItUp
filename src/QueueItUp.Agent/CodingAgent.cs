using Microsoft.SemanticKernel;
using QueueItUp.Abstractions;
using QueueItUp.Agent.Plugins;

namespace QueueItUp.Agent;

/// <summary>
/// A coding agent that uses the FileSystem plugin to solve coding tasks.
/// Outputs file changes and continues in a loop until completion is signaled.
/// </summary>
public class CodingAgent : AgentBase
{
    private readonly FileSystemPlugin _fileSystemPlugin;
    private readonly CodingAgentCompletionPlugin _completionPlugin;
    private readonly int _maxIterations;
    
    private const string FileSystemPluginName = "FileSystem";
    private const string CompletionPluginName = "Completion";

    public CodingAgent(string input, Kernel kernel, string basePath, int maxIterations = 10) : base(input, kernel)
    {
        _fileSystemPlugin = new FileSystemPlugin(basePath);
        _completionPlugin = new CodingAgentCompletionPlugin();
        _maxIterations = maxIterations;
        
        // Add plugins to the kernel
        AddPlugin(_fileSystemPlugin, FileSystemPluginName);
        AddPlugin(_completionPlugin, CompletionPluginName);
        
        // Set up system message
        AddSystemMessage($@"You are a coding agent. Your task is to solve coding problems by:
1. Using the {FileSystemPluginName} plugin to list and read files
2. Understanding the code structure
3. Making necessary changes to files using UpdateFile
4. Writing small comments before each action like 'I'll now generate the method XY so it can do ABC'
5. When you're done, call the {CompletionPluginName}.MarkComplete function with a description of what you accomplished

Always think step by step and explain what you're doing.");
    }

    public override async Task<string> ExecuteAsync(ITaskExecutionContext context, CancellationToken cancellationToken)
    {
        AddUserMessage(Input);
        
        var iterations = 0;
        var results = new List<string>();
        
        while (!_completionPlugin.IsCompleted && iterations < _maxIterations)
        {
            iterations++;
            results.Add($"--- Iteration {iterations} ---");
            
            var response = await GetLLMResponseAsync(cancellationToken);
            results.Add(response);
            
            if (_completionPlugin.IsCompleted)
            {
                results.Add($"Task completed: {_completionPlugin.CompletionDescription}");
                break;
            }
            
            // Check if we've hit the iteration limit
            if (iterations >= _maxIterations)
            {
                results.Add($"Warning: Maximum iterations ({_maxIterations}) reached. Task may not be complete.");
            }
        }
        
        var output = string.Join("\n\n", results);
        return output;
    }
}
