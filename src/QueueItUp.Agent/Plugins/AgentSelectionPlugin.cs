using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace QueueItUp.Agent.Plugins;

public class AgentSelectionPlugin
{
    public enum AgentNames
    {
        CodingAgent
    }

    public AgentSelectionResult? Result;
    public class AgentSelectionResult
    {
        public AgentNames AgentName { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    [KernelFunction, Description("Selects which agent should handle the task and provides a description for the agent.")]
    public void SelectAgent(
        [Description("Name of agent to select")] string agent, [Description("The request to the selected agent")] string requestToAgent)
    {
        // This is a stub. The LLM will fill this in.
        Result = new AgentSelectionResult
        {
            AgentName = Enum.Parse<AgentNames>(agent, true),
            Description = requestToAgent
        };
    }
}
