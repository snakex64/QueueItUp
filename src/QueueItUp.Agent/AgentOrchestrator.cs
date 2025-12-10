using Microsoft.SemanticKernel;
using QueueItUp.Abstractions;
using QueueItUp.Agent.Plugins;

namespace QueueItUp.Agent;

/// <summary>
/// The brain of the operation. Asks the LLM to choose which agent to use for the next step
/// and queues up the appropriate task.
/// </summary>
public class AgentOrchestrator : AgentBase
{
    private readonly FileSystemPlugin _fileSystemPlugin;
    private readonly string _basePath;

    public AgentOrchestrator(string input, Kernel kernel, string basePath) : base(input, kernel)
    {
        _basePath = basePath;
        _fileSystemPlugin = new FileSystemPlugin(basePath);
        
        // Add plugins to help determine which agent to use
        AddPlugin(_fileSystemPlugin, "FileSystem");
        
        // Set up system message
        AddSystemMessage(@"You are an orchestrator agent. Your job is to:
1. Analyze the user's request/instruction
2. Use available tools like FileSystem to understand the context
3. Decide which specialized agent should handle this task:
   - CodingAgent: For tasks that involve writing, modifying, or fixing code
   - (More agents can be added in the future)
4. Provide a clear, detailed description of what the chosen agent should do

Respond in the following format:
AGENT: <agent_name>
DESCRIPTION: <detailed description of what the agent should do>

Example:
AGENT: CodingAgent
DESCRIPTION: Create a new class called UserService in the src/Services directory that handles user authentication and includes methods for login, logout, and password reset.");
    }

    public override async Task<string> ExecuteAsync(ITaskExecutionContext context, CancellationToken cancellationToken)
    {
        AddUserMessage(Input);
        
        var response = await GetLLMResponseAsync(cancellationToken);
        
        // Parse the response to determine which agent to use
        var (agentName, description) = ParseOrchestratorResponse(response);
        
        if (string.IsNullOrEmpty(agentName) || string.IsNullOrEmpty(description))
        {
            return $"Error: Could not determine which agent to use.\nLLM Response: {response}";
        }
        
        // Queue the appropriate agent task
        string taskId;
        
        switch (agentName.ToLowerInvariant())
        {
            case "codingagent":
                var codingAgent = new CodingAgent(description, Kernel, _basePath);
                await context.Queue.EnqueueSubTaskAsync(codingAgent, this.Id, cancellationToken);
                taskId = codingAgent.Id;
                break;
            default:
                return $"Error: Unknown agent type '{agentName}'.\nAvailable agents: CodingAgent";
        }
        
        return $"Orchestrator Decision:\nAgent: {agentName}\nDescription: {description}\nTask ID: {taskId}";
    }

    private (string agentName, string description) ParseOrchestratorResponse(string response)
    {
        const string AgentPrefix = "AGENT:";
        const string DescriptionPrefix = "DESCRIPTION:";
        
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        
        string? agentName = null;
        string? description = null;
        
        foreach (var line in lines)
        {
            if (line.StartsWith(AgentPrefix, StringComparison.OrdinalIgnoreCase))
            {
                agentName = line.Substring(AgentPrefix.Length).Trim();
            }
            else if (line.StartsWith(DescriptionPrefix, StringComparison.OrdinalIgnoreCase))
            {
                description = line.Substring(DescriptionPrefix.Length).Trim();
            }
        }
        
        return (agentName ?? string.Empty, description ?? string.Empty);
    }
}
