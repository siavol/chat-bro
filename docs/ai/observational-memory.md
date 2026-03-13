# Observational Memory

ChatBro maintains per-user **observational memory** that persists durable facts across conversations without unbounded prompt growth.

## Architecture

### Memory Model

Each user's memory (`UserMemory`) contains two collections stored in Redis at key `chatbro:memory:{userId}` (no TTL):

- **Observations** — compressed facts extracted by an observer LLM (name, preferences, habits, ongoing tasks). Each has a `Timestamp`, `Text`, and `Importance` marker (🔴 high / 🟡 medium / 🟢 low).
- **Raw Messages** — recent unprocessed user/assistant turns. Each captures the user's message and the assistant's final response (not internal tool calls).

### Flow per Chat Turn

```
User message
    │
    ▼
Memory.Load (Redis GET)
    │
    ▼
Set ObservationalMemoryContext.Current
    │
    ▼
Agent run (orchestrator + domain agents see memory via MemoryAIContextProvider)
    │
    ▼
Append RawMessage (user msg + assistant response)
    │
    ▼
Memory.Save (Redis SET)
    │
    ├── if raw_messages >= ObserverRawMessageThreshold
    │       ▼
    │   Memory.Observe (LLM call → extract observations, clear raw messages)
    │       ▼
    │   Memory.Save
    │       │
    │       ├── if observations >= ReflectorObservationThreshold
    │       │       ▼
    │       │   Memory.Reflect (LLM call → prune/deduplicate observations)
    │       │       ▼
    │       │   Memory.Save
    │       │
    │       └── (done)
    │
    └── (done)
```

### Prompt Injection

`MemoryAIContextProvider` reads from `ObservationalMemoryContext` (a DI singleton wrapping `AsyncLocal<UserMemory?>`) and returns a system message injected into all agents:

```
## Observational Memory
### Observations
- 🔴 [2026-03-13] User's name is Marcus, vegetarian, lives in Tampere
- 🟡 [2026-03-13] User prefers dark mode in all tools
### Recent Unprocessed Messages
- [2026-03-13] User: "I like Thai food" → Assistant: "Got it!"
```

A single `MemoryAIContextProvider` instance is added to the orchestrator and every domain agent's `AIContextProviders` array in `AIAgentProvider`.

### Resilience

Both observer and reflector LLM calls are wrapped in try/catch — failures are logged as warnings and do not block the user turn. Raw messages are preserved on observer failure.

## Configuration

Settings in `appsettings.json` under `Chat:ObservationalMemory`:

| Setting | Default | Description |
|---------|---------|-------------|
| `ObserverRawMessageThreshold` | `20` | Raw message count before observer compresses them |
| `ReflectorObservationThreshold` | `50` | Observation count before reflector prunes them |

## Prompt Files

- [contexts/memory/observer.md](../../src/ChatBro.Server/contexts/memory/observer.md) — instructs the LLM to extract durable facts, use importance markers, avoid secrets
- [contexts/memory/reflector.md](../../src/ChatBro.Server/contexts/memory/reflector.md) — instructs the LLM to deduplicate, consolidate, remove outdated facts, target 30% reduction

## Commands

| Command | Effect |
|---------|--------|
| `/reset` | Clears chat threads only — memory preserved |
| `/reset_hard` | Clears chat threads **and** observational memory |
| `/show_memory` | Displays current observations and unprocessed message count |

## OpenTelemetry Spans

All operations emit spans from `ActivitySource("ChatBro.ObservationalMemory")`, matched by the existing `ChatBro.*` wildcard in ServiceDefaults.

| Span Name | Tags | Description |
|-----------|------|-------------|
| `Memory.Load` | `memory.user_id`, `memory.observations.count`, `memory.raw_messages.count` | Load memory from Redis |
| `Memory.Save` | `memory.user_id`, `memory.observations.count`, `memory.raw_messages.count` | Save memory to Redis |
| `Memory.Delete` | `memory.user_id`, `memory.observations.count`, `memory.raw_messages.count` | Delete memory from Redis |
| `Memory.Observe` | `memory.observer.input_raw_messages`, `memory.observer.input_observations`, `memory.observer.output_observations` | Observer LLM compression |
| `Memory.Reflect` | `memory.reflector.before_observations`, `memory.reflector.after_observations` | Reflector LLM pruning |

## Key Files

| File | Purpose |
|------|---------|
| `Services/AI/Memory/UserMemory.cs` | Data model (`UserMemory`, `Observation`, `RawMessage`) |
| `Services/AI/Memory/MemoryActivitySource.cs` | Shared `ActivitySource` and span/tag constants |
| `Services/AI/Memory/IObservationalMemoryStore.cs` | Storage interface |
| `Services/AI/Memory/RedisObservationalMemoryStore.cs` | Redis implementation |
| `Services/AI/Memory/ObservationalMemoryContext.cs` | `AsyncLocal` carrier (DI singleton) |
| `Services/AI/Memory/MemoryAIContextProvider.cs` | Injects memory into agent prompts |
| `Services/AI/Memory/IObserverService.cs` | Observer interface |
| `Services/AI/Memory/ObserverService.cs` | Observer LLM call implementation |
| `Services/AI/Memory/IReflectorService.cs` | Reflector interface |
| `Services/AI/Memory/ReflectorService.cs` | Reflector LLM call implementation |
| `Options/ObservationalMemorySettings.cs` | Configuration options |
