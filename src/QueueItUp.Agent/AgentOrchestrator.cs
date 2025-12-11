using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using QueueItUp.Abstractions;
using QueueItUp.Agent.Plugins;

namespace QueueItUp.Agent;

/// <summary>
/// The brain of the operation. Asks the LLM to choose which agent to use for the next step
/// and queues up the appropriate task.
/// </summary>
public class AgentOrchestrator : AgentBase, ITaskExecutable
{

    private readonly FileSystemPlugin _fileSystemPlugin;
    private readonly AgentSelectionPlugin _agentSelectionPlugin;
    private readonly string _basePath;

    public AgentOrchestrator(string input, Kernel kernel, string basePath) : base(input, kernel)
    {
        _basePath = basePath;
        _fileSystemPlugin = new FileSystemPlugin(basePath);
        _agentSelectionPlugin = new AgentSelectionPlugin();

        AddPlugin(_agentSelectionPlugin, "AgentSelection");

        AddSystemMessage(@"You are an orchestrator agent. Your job is to:
1. Analyze the user's request/instruction
2. Call the SelectAgent plugin function to select which specialized agent should handle this task and provide a description for the agent.
   - CodingAgent: For tasks that involve writing, modifying, or fixing code
   - (More agents can be added in the future)
3. Do not try to do the task yourself, ALWAYS delegate. Never code anything yourself
4. When calling a plugin, talk in the future. The plugin will only be called after. i.e. do not say 'file was updated' at the same time as trying to update it, wait to get a response back from the plugin

Once you have selected an agent, the software will queue up the appropriate agent task, only ever call that tool once. Stop working once the SelectAgent function has been called.
");
    }


    public override async Task<string> ExecuteAsync(ITaskExecutionContext context, CancellationToken cancellationToken)
    {
        AddUserMessage(Input);

        int i = 0;
        while (_agentSelectionPlugin.Result == null)
        {
            var response = await GetLLMResponseAsync(false, cancellationToken);
            Console.WriteLine(response);

            if (i == 10)
            {
                return "Error: Unable to determine which agent to use after multiple attempts.";
            }
        }

        // Queue the appropriate agent task
        string taskId;

        var scope = Kernel.Services.CreateScope();
        switch (_agentSelectionPlugin.Result.AgentName)
        {
            case AgentSelectionPlugin.AgentNames.CodingAgent:
                var codingAgent = new CodingAgent(_agentSelectionPlugin.Result.Description, scope.ServiceProvider.GetRequiredService<Kernel>(), _basePath);
                await context.Queue.EnqueueSubTaskAsync(codingAgent, this.Id, cancellationToken);
                taskId = codingAgent.Id;
                break;
            default:
                return $"Error: Unknown agent type '{_agentSelectionPlugin.Result.AgentName}'.\nAvailable agents: CodingAgent";
        }

        return $"Orchestrator Decision:\nAgent: {_agentSelectionPlugin.Result.AgentName}\nDescription: {_agentSelectionPlugin.Result.Description}\nTask ID: {taskId}";
    }


    // No longer needed: ParseOrchestratorResponse
}
