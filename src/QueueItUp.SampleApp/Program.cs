using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using QueueItUp.Abstractions;
using QueueItUp.Agent;
using QueueItUp.Core;
using QueueItUp.InMemory;

// Set up configuration
var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();
// 1. Setup the Transport
// This command launches the Docker Gateway, which aggregates all your enabled tools
var transport = new HttpClientTransport(new HttpClientTransportOptions()
{
    Endpoint = new Uri("http://localhost:8080/sse")
});

// 2. Connect the Client
await using var mcpClient = await McpClient.CreateAsync(transport);

// 3. Get the Tools (This will list GitHub, Postgres, etc. - whatever is in your Toolkit)
var mcpTools = await mcpClient.ListToolsAsync();
var functions = mcpTools.Select(tool => tool.AsKernelFunction()).ToList();

// Set up DI
var services = new ServiceCollection();
services.AddSingleton<IConfiguration>(config);
services.AddSingleton<IChatCompletionService>(serviceProvider =>
{
    var loggerFactory = serviceProvider.GetService<ILoggerFactory>();

    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var endpoint = configuration["OpenAI:Endpoint"] ?? "http://localhost:11434";
    var model = configuration["OpenAI:Model"] ?? throw new Exception("Model not configured");

#pragma warning disable SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    return new OpenAIChatCompletionService(model, new Uri(endpoint), "abc", loggerFactory: loggerFactory);
#pragma warning restore SKEXP0010 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
});
services.AddScoped<Kernel>(sp =>
{
    var plugins = new KernelPluginCollection();
    plugins.AddFromFunctions("mcp", functions);

    var kernel = new Kernel(sp, plugins);

    return kernel;
});
services.AddSingleton<ITaskQueue, InMemoryTaskQueue>();
services.AddSingleton<TaskExecutionContext>();

var provider = services.BuildServiceProvider();
var queue = provider.GetRequiredService<ITaskQueue>();
var context = provider.GetRequiredService<TaskExecutionContext>();
var kernel = provider.GetRequiredService<Kernel>();

// The base path for the file system plugin (use current directory for demo)
var basePath = @"C:\Users\pasc3\source\repos\QueueItUp";

// Create an AgentOrchestrator task with the desired instruction
var orchestratorTask = new AgentOrchestrator(
    "Use the coding agent to modify the file system plugin to allow deleting files and creating new files. Directly update the FileSystemPlugin.cs file",
    kernel: kernel,
    basePath: basePath
);

// Enqueue the orchestrator task
await queue.EnqueueAsync(orchestratorTask, CancellationToken.None);

// Loop and execute all tasks, logging every output
while (true)
{
    var executedTask = await context.ExecuteNextAsync(CancellationToken.None);
    if (executedTask is null)
        break;

    if (executedTask is ITaskExecutable exec)
    {
        Console.WriteLine($"Task {executedTask.GetType().Name} Output:\n{exec.StringOutput}\n");
    }
    else
    {
        Console.WriteLine($"Task {executedTask.GetType().Name} executed, but no string output available.\n");
    }
}
