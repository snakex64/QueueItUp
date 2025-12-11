# QueueItUp.Agent

The QueueItUp.Agent project provides AI agent capabilities using Semantic Kernel to integrate with Large Language Models (LLMs).

## Components

### AgentBase
Base class for all AI agents that provides:
- Semantic Kernel integration with `Kernel`, `ChatHistory`, and `IChatCompletionService`
- Plugin management through `AddPlugin<T>()` method
- LLM communication methods using chat conversations:
  - `AddSystemMessage()` - Add system messages
  - `AddUserMessage()` - Add user messages
  - `AddAssistantMessage()` - Add assistant messages
  - `SendMessageAsync()` - Send a message and get a response
  - `GetLLMResponseAsync()` - Get LLM response using current chat history

### CodingAgent
A specialized agent that solves coding tasks by:
- Using the FileSystemPlugin to list, read, and modify files
- Iterating in a loop until the task is complete
- Writing small comments before each action
- Calling `CodingAgentCompletionPlugin.MarkComplete()` when done

### AgentOrchestrator
The brain of the operation that:
- Analyzes user instructions
- Uses plugins like FileSystemPlugin to understand context
- Decides which specialized agent should handle the task
- Queues up the appropriate agent task with LLM-generated instructions

### FileSystemPlugin
Provides file system operations for agents:
- `ListFiles(pattern)` - Lists files matching glob patterns like `"src/*.cs"` or `"**/*.txt"`
- `ReadFile(filePath)` - Reads file content
- `UpdateFile(filePath, content)` - Updates or creates a file

### CodingAgentCompletionPlugin
Allows the CodingAgent to signal completion:
- `MarkComplete(description)` - Marks the task as complete with a summary

## Usage Example

```csharp
using Microsoft.SemanticKernel;
using QueueItUp.Agent;
using QueueItUp.InMemory;

// Setup Semantic Kernel with your LLM configuration
var builder = Kernel.CreateBuilder();
builder.AddOpenAIChatCompletion(
    modelId: "gpt-4",
    apiKey: "your-api-key");
var kernel = builder.Build();

// Create a task queue
var queue = new InMemoryTaskQueue();

// Option 1: Use AgentOrchestrator to decide which agent to use
var orchestrator = new AgentOrchestrator(
    "Create a new User class with Name and Email properties",
    kernel,
    basePath: "/path/to/your/project");
await queue.EnqueueAsync(orchestrator, CancellationToken.None);

// Option 2: Use CodingAgent directly
var codingAgent = new CodingAgent(
    "Add a new method called ValidateEmail to the User class",
    kernel,
    basePath: "/path/to/your/project");
await queue.EnqueueAsync(codingAgent, CancellationToken.None);

// Execute tasks
// The orchestrator will automatically queue sub-tasks
// The coding agent will loop until completion
```

## Configuration

Agents receive the Semantic Kernel `Kernel` instance through their constructor, which should be configured with:
- An LLM service (OpenAI, Azure OpenAI, etc.)
- Any necessary authentication and settings

The configuration is typically managed through dependency injection in your application.

## Design Patterns

- **Plugin-based**: Agents use plugins added by type to extend capabilities
- **Chat-based**: All LLM interactions use `ChatHistory` for context
- **Loop execution**: CodingAgent iterates until completion signal
- **Orchestration**: AgentOrchestrator delegates to specialized agents
- **Task queuing**: Agents can queue sub-tasks using `ITaskExecutionContext`
