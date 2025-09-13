<!--
Guidance for AI coding agents working on the QueueItUp repository.
Keep this file concise and focused on codebase-specific patterns, conventions, and run/build commands.
-->
# QueueItUp — Copilot Instructions

This repository is a small C# solution that demonstrates a pluggable, strongly-typed task queue. The goal of these notes is to help an AI contributor be productive quickly by highlighting architecture, key files, conventions, and developer workflows.

1. Big picture
   - Solution layout: see `src/` with projects: `QueueItUp.Core`, `QueueItUp.Abstractions`, `QueueItUp.InMemory`, `QueueItUp.Db`, `QueueItUp.Api`, and `QueueItUp.SampleApp`.
   - Responsibility separation:
     - `QueueItUp.Abstractions`: lightweight interfaces and types used across projects (`ITask<TIn,TOut>`, `ITaskInfo<TIn,TOut>`, `TaskStatus`).
     - `QueueItUp.Core`: abstract base implementations and helpers (e.g. `TaskBase<TIn,TOut>`).
     - `QueueItUp.InMemory`: a simple, thread-safe in-memory queue implementation used by the sample app.
     - `QueueItUp.Db` / `QueueItUp.Api`: intentionally minimal/stubbed — used to show where persistence or API-based queueing would be implemented.
     - `QueueItUp.SampleApp`: a small console demo showing enqueue/dequeue/execute flows.

2. What to change and why (common tasks for agents)
   - Adding new tasks: subclass `TaskBase<TInput,TOutput>` in `QueueItUp.Core` or `SampleApp` for examples. Example: `PrintTask` in `src/QueueItUp.SampleApp/Program.cs`.
   - Implementing a queue backend: follow the `QueueItUp.InMemory/InMemoryTaskQueue.cs` pattern (thread-safe queue + generic enqueue/dequeue). Persisted or API-backed queues go in `QueueItUp.Db` or `QueueItUp.Api` respectively.
   - Keep `Abstractions` minimal and stable: other projects reference it. Avoid moving breaking changes into `QueueItUp.Abstractions` without updating dependents and tests.

3. Notable code patterns and conventions
   - Strong generics: tasks expose typed Input/Output via `ITask<TIn,TOut>` and `TaskBase<TIn,TOut>`; follow that pattern for new tasks and queue methods.
   - Read-only inspection: `ITaskInfo<TIn,TOut>` exposes `LoadInputAsync` and `LoadOutputAsync` to support lazy loading of payloads (useful for DB-backed implementations).
   - Minimal dependencies: projects are small and avoid heavy frameworks — prefer using plain .NET types and simple async Task-based APIs.
   - Project stubs: `QueueItUp.Db` and `QueueItUp.Api` are expected to be implemented later; when adding features, update these projects only if the change requires persistence or network behavior.

4. Build / run / test (developer workflows)
   - Build the solution from repo root with dotnet CLI: `dotnet build QueueItUp.sln`.
   - Run the sample app: `dotnet run --project src/QueueItUp.SampleApp/QueueItUp.SampleApp.csproj`.
   - There are no automated tests in the repo. Add unit tests under a new test project when changing core behavior.

5. Examples (use these as references in PRs)
   - Task implementation: `src/QueueItUp.SampleApp/Program.cs` — `PrintTask : TaskBase<string,bool>`.
   - In-memory queue: `src/QueueItUp.InMemory/InMemoryTaskQueue.cs` — shows how Enqueue/Dequeue should behave and casts to `ITaskInfo` for inspection.
   - Core base class: `src/QueueItUp.Core/TaskBase.cs` — shows lifecycle fields (`Id`, `Status`) and abstract execution/loading methods.

6. Suggested PR checklist for agents
   - Small focused changes: add/modify one project at a time and ensure `QueueItUp.Abstractions` consumers are updated.
   - Preserve strong typing: prefer changing generics and signatures only when necessary; add overloads where possible.
   - Update README when adding major features (new queue backend, persistence, or API behavior).
   - Include a short example in `QueueItUp.SampleApp` or an additional sample app when introducing new end-to-end behavior.

7. When you are uncertain
   - If a change touches `QueueItUp.Abstractions`, run a local build of the whole solution to catch compile-time regressions.
   - For persistence or API changes, stub behavior in `QueueItUp.Db` / `QueueItUp.Api` and add integration notes in README.

If anything here is unclear or you'd like more detail in a particular area (e.g., expected DB semantics or task retry behavior), tell me which area and I'll expand this guidance.
