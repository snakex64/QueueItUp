using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using QueueItUp.Abstractions;
using QueueItUp.Core;

namespace QueueItUp.Agent;

/// <summary>
/// Base class for AI agents that use Semantic Kernel to communicate with LLMs.
/// Provides simple accessors to send requests to LLMs and add plugins.
/// </summary>
public abstract class AgentBase : TaskBase<string, string>
{
    protected Kernel Kernel { get; private set; }
    protected ChatHistory ChatHistory { get; private set; }
    protected IChatCompletionService ChatCompletionService { get; private set; }

    protected AgentBase(string input, Kernel kernel) : base(input)
    {
        Kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        ChatHistory = new ChatHistory();
        ChatCompletionService = Kernel.GetRequiredService<IChatCompletionService>();
    }

    /// <summary>
    /// Adds a plugin to the kernel by type.
    /// </summary>
    protected void AddPlugin<T>(T plugin, string? pluginName = null) where T : class
    {
        Kernel.Plugins.AddFromObject(plugin, pluginName);
    }

    /// <summary>
    /// Adds a system message to the chat history.
    /// </summary>
    protected void AddSystemMessage(string message)
    {
        ChatHistory.AddSystemMessage(message);
    }

    /// <summary>
    /// Adds a user message to the chat history.
    /// </summary>
    protected void AddUserMessage(string message)
    {
        ChatHistory.AddUserMessage(message);
    }

    /// <summary>
    /// Adds an assistant message to the chat history.
    /// </summary>
    protected void AddAssistantMessage(string message)
    {
        ChatHistory.AddAssistantMessage(message);
    }

    /// <summary>
    /// Sends a request to the LLM using the current chat history and returns the response.
    /// </summary>
    protected async Task<string> GetLLMResponseAsync(CancellationToken cancellationToken = default)
    {
        var result = await ChatCompletionService.GetChatMessageContentAsync(
            ChatHistory,
            kernel: Kernel,
            cancellationToken: cancellationToken);

        var responseText = result.Content ?? string.Empty;
        ChatHistory.AddAssistantMessage(responseText);
        return responseText;
    }

    /// <summary>
    /// Sends a message and gets a response from the LLM.
    /// </summary>
    protected async Task<string> SendMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        AddUserMessage(message);
        return await GetLLMResponseAsync(cancellationToken);
    }
}
