# QueueItUp

> **QueueItUp** is a highly abstract, flexible C# demo project for queuing and executing asynchronous tasks.

## Purpose

QueueItUp enables users to queue long-running asynchronous tasks, each of which may have many sub-tasks, and ensures they are executed in the correct order. The system is designed to be highly extensible, supporting multiple execution and storage modes, and to provide strong compile-time safety via generic typing.

## Key Design Requirements

1. **Immediate or Queued Execution**: Tasks can be executed immediately (for fast, idempotent operations) or queued for later/distributed execution. Failed tasks can be retried safely.
2. **Flexible Distribution**: Tasks may be limited in concurrency or distributed to other systems (e.g., a PC queues a task for a server with special access).
3. **Dynamic Subtasking**: Tasks can spawn sub-tasks dynamically, forming complex dependency trees (e.g., a "Search web" task queues "Google Search", then for each result, queues "Get page from cache", and so on).
4. **Parameter and Result Passing**: Tasks and sub-tasks can pass parameters and analyze results in a type-safe way.
5. **Strong Typing**: All tasks have generic input/output types, ensuring compile-time validation and eliminating manual serialization/deserialization.
6. **Pluggable Architecture**: The system supports multiple running modes:
	- In-memory only
	- Queue stored in DB, running in-memory
	- Queue in external process (API), no DB
	- Queue in external process (API), stored in DB

## Solution Structure

```
QueueItUp/
├── src/
│   ├── QueueItUp.Core/        # Core abstractions: interfaces, base classes, generics
│   ├── QueueItUp.InMemory/    # In-memory queue implementation
│   ├── QueueItUp.Db/          # DB-backed queue implementation (stub)
│   ├── QueueItUp.Api/         # API-based queue implementation (stub)
│   └── QueueItUp.SampleApp/   # Example app demonstrating usage
├── QueueItUp.sln              # Solution file
└── README.md                  # This documentation
```

## Project Documentation

### QueueItUp.Core

Defines the core abstractions:

- `ITask<TInput, TOutput>`: Generic interface for a task with typed input/output, subtask management, and result analysis.
- `TaskBase<TInput, TOutput>`: Abstract base class implementing subtask management and providing extension points for execution and result analysis.
- `ITaskQueue`: Abstraction for a task queue, supporting enqueue/dequeue of generic tasks.

### QueueItUp.InMemory

In-memory implementation of `ITaskQueue` using a thread-safe queue. Suitable for fast, ephemeral, or test scenarios.

### QueueItUp.Db

Stub for a DB-backed queue implementation. Intended to persist queued tasks and support recovery after restarts.

### QueueItUp.Api

Stub for an API-based queue implementation. Intended for distributed scenarios where tasks are queued and managed via HTTP or other APIs.

### QueueItUp.SampleApp

Demonstrates how to define a custom task, enqueue it, and execute it using the in-memory queue.

## Example: Defining and Running a Task

```csharp
public class PrintTask : TaskBase<string, bool>
{
	public PrintTask(string input) : base(input) { }
	public override Task<bool> ExecuteAsync(CancellationToken cancellationToken = default)
	{
		Console.WriteLine($"Executing PrintTask: {Input}");
		return Task.FromResult(true);
	}
}

// Usage
ITaskQueue queue = new InMemoryTaskQueue();
var task = new PrintTask("Hello, QueueItUp!");
await queue.EnqueueAsync(task);
var dequeued = await queue.DequeueAsync();
if (dequeued != null)
{
	await dequeued.ExecuteAsync();
}
```

## Extending the System

- Implement new `ITaskQueue` variants for DB, API, or hybrid scenarios.
- Define new `TaskBase` subclasses for custom business logic.
- Compose tasks and sub-tasks to model complex workflows.

## Future Work

- Implement the DB and API queue backends.
- Add support for task persistence, retries, and distributed execution.
- Provide more advanced sample tasks and orchestration patterns.

---

**QueueItUp** is designed for maximum flexibility, extensibility, and type safety in asynchronous task orchestration.