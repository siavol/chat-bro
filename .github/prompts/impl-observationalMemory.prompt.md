# Feature: ChatBro Observational Memory

## Summary

Add per-user observational memory that persists durable facts across conversations without unbounded prompt growth. The system maintains two text blocks per user in Redis: **observations** (compressed facts extracted by an observer LLM) and **raw messages** (recent unprocessed turns). Memory is injected into all agent prompts (orchestrator + domain agents) via a shared `MemoryAIContextProvider` using `AsyncLocal<UserMemory>` to bridge the scoped `ChatService` and singleton agents. When raw messages exceed ~20 entries, a synchronous observer LLM call compresses them into observations. When observations exceed ~50 entries, a reflector LLM call prunes/deduplicates. Both are resilient — failures don't block the user turn. `/reset` clears memory alongside chat threads. All memory operations emit OpenTelemetry spans from the start, and Aspire dashboard tracing is used as a validation tool throughout.

**Key architectural decisions:**
- `AsyncLocal<UserMemory>` carries per-request memory from `ChatService` into singleton `MemoryAIContextProvider`
- Single `MemoryAIContextProvider` instance added to all agents' `AIContextProviders` arrays in `AIAgentProvider`
- Raw messages capture only user message + final assistant response (not internal tool calls)
- Observer/reflector prompts stored as markdown files under `contexts/memory/` following existing conventions
- Redis key: `chatbro:memory:{userId}` with no TTL
- `ActivitySource("ChatBro.ObservationalMemory")` used from Phase 1 onward (already matched by `ChatBro.*` wildcard in [src/ChatBro.ServiceDefaults/Extensions.cs](src/ChatBro.ServiceDefaults/Extensions.cs))
- Message-count based thresholds: observer at 20 raw messages, reflector at 50 observations

## Phases

### Phase 1: Memory Model, Options, Storage Service, and ActivitySource
**Status**: ✅ Complete

**Goal**: Establish the foundational data model, configuration, Redis persistence, and the shared `ActivitySource` for observational memory. Every subsequent phase will use this `ActivitySource` to emit spans.

**Required Results**:
- `UserMemory` record type with observations list and raw messages list
- `ObservationalMemorySettings` options class with configurable thresholds
- `IObservationalMemoryStore` interface + `RedisObservationalMemoryStore` implementation with OTEL spans on all operations
- `ActivitySource("ChatBro.ObservationalMemory")` established and shared
- Configuration wired in appsettings

**Validation Criteria**:
- [x] Project compiles without errors
- [x] `ObservationalMemorySettings` binds from `Chat:ObservationalMemory` config section with validation

**Tasks**:
- [x] Create [src/ChatBro.Server/Services/AI/Memory/MemoryActivitySource.cs](src/ChatBro.Server/Services/AI/Memory/MemoryActivitySource.cs) — static class exposing `ActivitySource Source = new("ChatBro.ObservationalMemory")` and constants for span names (`Memory.Load`, `Memory.Save`, `Memory.Delete`, `Memory.Observe`, `Memory.Reflect`) and tag keys (`memory.user_id`, `memory.observations.count`, `memory.raw_messages.count`)
- [x] Create [src/ChatBro.Server/Options/ObservationalMemorySettings.cs](src/ChatBro.Server/Options/ObservationalMemorySettings.cs) with properties: `ObserverRawMessageThreshold` (int, default 20), `ReflectorObservationThreshold` (int, default 50), `Enabled` (bool, default true)
- [x] Create `UserMemory` model in [src/ChatBro.Server/Services/AI/Memory/UserMemory.cs](src/ChatBro.Server/Services/AI/Memory/UserMemory.cs) — record with `List<Observation>` (each: `DateTimeOffset Timestamp`, `string Text`, `string Importance`) and `List<RawMessage>` (each: `DateTimeOffset Timestamp`, `string UserMessage`, `string AssistantResponse`)
- [x] Create `IObservationalMemoryStore` interface in [src/ChatBro.Server/Services/AI/Memory/IObservationalMemoryStore.cs](src/ChatBro.Server/Services/AI/Memory/IObservationalMemoryStore.cs) with methods: `LoadAsync(userId)`, `SaveAsync(userId, memory)`, `DeleteAsync(userId)`
- [x] Create `RedisObservationalMemoryStore` in [src/ChatBro.Server/Services/AI/Memory/RedisObservationalMemoryStore.cs](src/ChatBro.Server/Services/AI/Memory/RedisObservationalMemoryStore.cs) using `IConnectionMultiplexer`, key `chatbro:memory:{userId}`, no TTL, JSON serialization. Wrap each method in an `Activity` from `MemoryActivitySource.Source` — set tags `memory.user_id`, `memory.observations.count`, `memory.raw_messages.count`. Use `ActivityExtensions.SetException()` from [src/ChatBro.ServiceDefaults/ActivityExtensions.cs](src/ChatBro.ServiceDefaults/ActivityExtensions.cs) on failure
- [x] Add `ObservationalMemorySettings` options registration in [src/ChatBro.Server/DependencyInjection/AgentsAiExtensions.cs](src/ChatBro.Server/DependencyInjection/AgentsAiExtensions.cs) — bind `Chat:Memory`, validate data annotations
- [x] Register `IObservationalMemoryStore` as singleton in `AgentsAiExtensions.AddAgents()`
- [x] Add `Memory` section to [src/ChatBro.Server/appsettings.json](src/ChatBro.Server/appsettings.json) under `Chat` with default thresholds

---

### Phase 2: Memory Prompt Injection Infrastructure
**Status**: ✅ Code Complete (runtime unverified)

**Goal**: Wire memory into all agent prompts so the orchestrator and every domain agent can see the user's observations and recent raw messages. The memory load span from Phase 1 will now appear in every chat request trace.

**Required Results**:
- `ObservationalMemoryContext` static class with `AsyncLocal<UserMemory?>`
- `MemoryAIContextProvider` that reads from `AsyncLocal` and returns a system message
- Provider added to all agents in `AIAgentProvider`
- `ChatService` loads memory (with OTEL span) and sets context before agent run

**Validation Criteria**:
- [x] Project compiles without errors
- [ ] Store can load `UserMemory` from Redis (verifiable via `/debug/chat` or manual Redis inspection) — `Memory.Load` span appears in Aspire dashboard
- [ ] When memory exists in Redis, Aspire OTEL traces show a `Memory.Load` span followed by the memory content appearing in `gen_ai.input.messages` for the orchestrator
- [ ] Domain agent traces also contain the memory system message
- [ ] When no memory exists, agents function normally — `Memory.Load` span shows zero counts in tags

**Tasks**:
- [x] Create [src/ChatBro.Server/Services/AI/Memory/ObservationalMemoryContext.cs](src/ChatBro.Server/Services/AI/Memory/ObservationalMemoryContext.cs) — static class with `AsyncLocal<UserMemory?>` property (`Current` getter/setter)
- [x] Create [src/ChatBro.Server/Services/AI/Memory/MemoryAIContextProvider.cs](src/ChatBro.Server/Services/AI/Memory/MemoryAIContextProvider.cs) extending `AIContextProvider` — in `ProvideAIContextAsync`, read `ObservationalMemoryContext.Current`, format observations + raw messages into a system message, return `AIContext`. If null/empty, return empty `AIContext`
- [x] Define the memory prompt template (inline or constants class) — structure: `## Observational Memory` / `### Observations` / `{entries}` / `### Recent Unprocessed Messages` / `{entries}`
- [x] Modify [src/ChatBro.Server/Services/AI/AIAgentProvider.cs](src/ChatBro.Server/Services/AI/AIAgentProvider.cs): create one `MemoryAIContextProvider` instance, add it to the `AIContextProviders` array of the orchestrator, restaurants agent, and documents agent (append to existing providers)
- [x] Modify [src/ChatBro.Server/Services/AI/ChatService.cs](src/ChatBro.Server/Services/AI/ChatService.cs) `GetChatResponseAsync`: inject `IObservationalMemoryStore`, before `chatAgent.RunAsync` call `store.LoadAsync(userId)` (this already emits a `Memory.Load` span from Phase 1), set `ObservationalMemoryContext.Current = memory`, wrap agent run in try/finally to clear the context

---

### Phase 3: Raw Message Capture
**Status**: 🔲 Not Started

**Goal**: After each agent turn, persist the user's message and the assistant's response as raw messages in the memory store, building up the buffer for the observer. The `Memory.Save` span confirms persistence.

**Required Results**:
- Each successful turn appends a `RawMessage` to the user's memory
- Memory is saved back to Redis (with OTEL span) after appending

**Validation Criteria**:
- [ ] After sending 3 messages via `/debug/chat`, Aspire traces show `Memory.Load` and `Memory.Save` spans on every request, with `memory.raw_messages.count` incrementing (1, 2, 3)
- [ ] Redis key `chatbro:memory:debug` (for debug user) contains raw messages verified via RedisInsight

**Tasks**:
- [ ] In [src/ChatBro.Server/Services/AI/ChatService.cs](src/ChatBro.Server/Services/AI/ChatService.cs) `GetChatResponseAsync`, after `chatAgent.RunAsync` returns `response`: create a `RawMessage` with the user's `message`, `response.Text`, and `DateTimeOffset.UtcNow`
- [ ] Append to the loaded `UserMemory.RawMessages` list (or create new `UserMemory` if null)
- [ ] Call `IObservationalMemoryStore.SaveAsync(userId, memory)` to persist (this already emits a `Memory.Save` span from Phase 1)

---

### Phase 4: Observer Service
**Status**: 🔲 Not Started

**Goal**: Implement the observer that compresses raw messages into durable observations when the threshold is exceeded, then clears the raw messages buffer. Observer execution wrapped in its own OTEL span.

**Required Results**:
- `IObserverService` interface + implementation with LLM call and `Memory.Observe` OTEL span
- Observer prompt in `contexts/memory/observer.md`
- Observer triggered in `ChatService` after raw message capture when threshold exceeded
- Observations appended, raw messages cleared on success
- Failures caught and logged (turn still succeeds)

**Validation Criteria**:
- [ ] After sending 21+ messages via `/debug/chat`, Aspire traces show a `Memory.Observe` span on the turn that crosses the threshold
- [ ] The `Memory.Observe` span includes tags for input raw message count and output observation count
- [ ] After observer runs, `Memory.Save` span shows `memory.raw_messages.count = 0` and `memory.observations.count > 0`
- [ ] Redis confirms observations exist and raw messages are cleared
- [ ] If the observer LLM call is artificially failed (e.g., invalid API key in test), the chat turn still returns a response, a failed `Memory.Observe` span with exception is visible, and raw messages are preserved

**Tasks**:
- [ ] Create [src/ChatBro.Server/contexts/memory/observer.md](src/ChatBro.Server/contexts/memory/observer.md) — prompt instructing the LLM to: extract durable user facts/preferences/constraints, note ongoing tasks, use importance markers (🔴/🟡/🟢), avoid copying large tool outputs verbatim, **never persist secrets** (tokens, API keys, credentials), output as structured list
- [ ] Create `IObserverService` interface in [src/ChatBro.Server/Services/AI/Memory/IObserverService.cs](src/ChatBro.Server/Services/AI/Memory/IObserverService.cs) with `ObserveAsync(UserMemory memory, CancellationToken)` returning updated `UserMemory`
- [ ] Create `ObserverService` in [src/ChatBro.Server/Services/AI/Memory/ObserverService.cs](src/ChatBro.Server/Services/AI/Memory/ObserverService.cs) — wrap entire method in `MemoryActivitySource.Source.StartActivity("Memory.Observe")`. Load observer prompt from file, send raw messages + existing observations to LLM, parse response into new `Observation` entries, clear raw messages, return updated memory. Set span tags for input/output counts. Use `ActivityExtensions.SetException()` on failure. Uses `IChatClient` built from `OpenAIClient` + model config with `.UseOpenTelemetry()`
- [ ] Verify `contexts/memory/` is covered by the existing content copy glob in [src/ChatBro.Server/ChatBro.Server.csproj](src/ChatBro.Server/ChatBro.Server.csproj) (the existing `contexts/**` pattern should cover it; if not, add it)
- [ ] Register `IObserverService` in [src/ChatBro.Server/DependencyInjection/AgentsAiExtensions.cs](src/ChatBro.Server/DependencyInjection/AgentsAiExtensions.cs)
- [ ] In [src/ChatBro.Server/Services/AI/ChatService.cs](src/ChatBro.Server/Services/AI/ChatService.cs), after raw message capture: check if `memory.RawMessages.Count >= threshold`, if so call `observerService.ObserveAsync(memory)` inside try/catch, save result to store. On failure, log warning and continue

---

### Phase 5: Reflector Service
**Status**: 🔲 Not Started

**Goal**: Implement the reflector that prunes/deduplicates observations when they exceed the configured threshold. Reflector execution wrapped in its own OTEL span.

**Required Results**:
- `IReflectorService` interface + implementation with LLM call and `Memory.Reflect` OTEL span
- Reflector prompt in `contexts/memory/reflector.md`
- Reflector triggered after observer when observation threshold exceeded
- Failures caught and logged

**Validation Criteria**:
- [ ] After observations exceed 50 entries (can temporarily lower threshold for testing), Aspire traces show a `Memory.Reflect` span
- [ ] The `Memory.Reflect` span includes before/after observation counts as tags
- [ ] After reflector runs, `Memory.Save` span shows reduced `memory.observations.count`
- [ ] High-signal observations (🔴) are preserved; low-value/outdated ones are pruned (verify in Redis)
- [ ] Reflector failure produces a failed span with exception and doesn't block the turn

**Tasks**:
- [ ] Create [src/ChatBro.Server/contexts/memory/reflector.md](src/ChatBro.Server/contexts/memory/reflector.md) — prompt instructing the LLM to: deduplicate observations, remove outdated facts, preserve high-importance (🔴) items, consolidate related observations, output cleaned list
- [ ] Create `IReflectorService` interface in [src/ChatBro.Server/Services/AI/Memory/IReflectorService.cs](src/ChatBro.Server/Services/AI/Memory/IReflectorService.cs) with `ReflectAsync(UserMemory memory, CancellationToken)` returning updated `UserMemory`
- [ ] Create `ReflectorService` in [src/ChatBro.Server/Services/AI/Memory/ReflectorService.cs](src/ChatBro.Server/Services/AI/Memory/ReflectorService.cs) — wrap in `MemoryActivitySource.Source.StartActivity("Memory.Reflect")`. Load reflector prompt, send observations to LLM, replace observations with pruned result. Set span tags for before/after counts. Use `ActivityExtensions.SetException()` on failure. Uses `IChatClient` with `.UseOpenTelemetry()`
- [ ] Register `IReflectorService` in [src/ChatBro.Server/DependencyInjection/AgentsAiExtensions.cs](src/ChatBro.Server/DependencyInjection/AgentsAiExtensions.cs)
- [ ] In [src/ChatBro.Server/Services/AI/ChatService.cs](src/ChatBro.Server/Services/AI/ChatService.cs), after observer: check if `memory.Observations.Count >= reflectorThreshold`, if so call `reflectorService.ReflectAsync(memory)` inside try/catch, save result. On failure, log and continue

---

### Phase 6: Reset Integration
**Status**: 🔲 Not Started

**Goal**: Ensure `/reset` clears observational memory alongside chat threads. The `Memory.Delete` span confirms cleanup.

**Required Results**:
- `ResetChatAsync` deletes the user's memory from Redis (with OTEL span)
- User experiences a fully fresh assistant after reset

**Validation Criteria**:
- [ ] After `/reset`, Aspire trace shows a `Memory.Delete` span
- [ ] Redis key `chatbro:memory:{userId}` is deleted (verify in RedisInsight)
- [ ] Subsequent conversation shows no trace of prior observations — `Memory.Load` span shows zero counts

**Tasks**:
- [ ] Inject `IObservationalMemoryStore` into `ChatService` constructor (may already be done from Phase 2)
- [ ] In [src/ChatBro.Server/Services/AI/ChatService.cs](src/ChatBro.Server/Services/AI/ChatService.cs) `ResetChatAsync`: add call to `IObservationalMemoryStore.DeleteAsync(userId)` alongside existing thread deletion (this already emits a `Memory.Delete` span from Phase 1)

---

### Phase 7: End-to-End Validation and Documentation
**Status**: 🔲 Not Started

**Goal**: Verify the complete feature works end-to-end against all acceptance criteria from the issue, and document the implementation.

**Required Results**:
- All acceptance criteria met
- `docs/ai/observational-memory.md` created
- Full trace lifecycle visible in Aspire dashboard

**Validation Criteria**:
- [ ] After 20+ turns via `/debug/chat`, observations are generated — confirmed via `Memory.Observe` spans and Redis content
- [ ] After 50+ observations (use lowered threshold for practical testing), reflector prunes — confirmed via `Memory.Reflect` spans
- [ ] `/reset` clears everything — `Memory.Delete` span, then next turn shows `Memory.Load` with zero counts
- [ ] Tool-heavy sessions (e.g., restaurant queries) don't cause runaway prompt growth; key facts persist via observations
- [ ] Full Aspire dashboard trace shows the lifecycle: `Memory.Load` → agent run → `Memory.Save` → (optional) `Memory.Observe` → `Memory.Save` → (optional) `Memory.Reflect` → `Memory.Save`

**Tasks**:
- [ ] Test via [src/ChatBro.Server/ChatBro.Server.http](src/ChatBro.Server/ChatBro.Server.http) — send 25+ messages with varied topics, verify observations appear
- [ ] Verify in Aspire dashboard that the full span tree is correct and all tags populate
- [ ] Test `/reset` clears the memory key
- [ ] Create [docs/ai/observational-memory.md](docs/ai/observational-memory.md) documenting the architecture (memory model, observer/reflector flow, configuration, prompt files, OTEL span reference)
- [ ] Update orchestrator prompt in [src/ChatBro.Server/contexts/orchestrator.md](src/ChatBro.Server/contexts/orchestrator.md) if needed — add a note that the agent has access to observational memory

---

## Implementation Observations & Notes

| Phase | Observation |
|-------|-------------|
| 1 | All 8 tasks complete. Build succeeds with 0 warnings, 0 errors. Two validation criteria (Redis round-trip, Aspire spans) are deferred to Phase 2+ when the store is actually invoked at runtime. |
| 2 | All 5 tasks complete. Build succeeds 0/0. `ObservationalMemoryContext` refactored from static to DI singleton per user feedback. `MemoryAIContextProvider` receives it via constructor. `ChatService` loads memory before agent run and clears `AsyncLocal` in finally. **Runtime validation not performed** — 4 criteria (Aspire spans, gen_ai.input.messages, domain traces, empty-memory behavior) require AppHost running and were not tested. |

## Prompt Reflections & Adjustments

| User Correction | Root Cause | Suggested Prompt Improvement |
|----------------|------------|------------------------------|
| OTEL should not be a separate phase; integrate into each phase | Original plan treated OTEL as cross-cutting but deferred it, preventing its use as a validation tool during development | Planning prompt should instruct: "Cross-cutting concerns like observability should be integrated into each phase, not deferred. If a concern enables validation of other phases, it must be introduced early." |
| Phase 1 had runtime validation criteria (Redis round-trip, Aspire spans) but no code path that calls the store | Plan separated infrastructure creation from pipeline wiring, then assigned runtime criteria to the infrastructure-only phase | Every phase must include a call path for what it builds. Validation criteria must be achievable using only artifacts produced by that phase. Before finalizing a phase, check: "Can I trigger every validation criterion by running the app after only this phase's changes?" If not, move the criterion to the phase that enables it. |
| Phase 2 marked complete with 4 of 5 runtime validation criteria unchecked — only compilation was verified | Plan lists runtime criteria (Aspire spans, gen_ai.input.messages) but doesn't instruct the implementer to start the AppHost and actually run `/debug/chat`. Criteria without execution instructions become aspirational, not actionable. | For each runtime validation criterion, include a concrete execution step: which command to run, which endpoint to hit, what to look for in the dashboard. Separate criteria into "compile-time" (checked by build) and "runtime" (checked by running the app) groups. Mark a phase complete only when ALL criteria are verified — if runtime testing is skipped, the phase status should be "code complete, runtime unverified". |
| Phase 2 plan specified `ObservationalMemoryContext` as a static class; user corrected to DI singleton | Plan defaulted to static class for `AsyncLocal` carrier without considering that DI-managed singletons are preferred in the codebase for testability and explicit dependency tracking. Static classes hide dependencies and make unit testing harder. | When the plan introduces a shared-state carrier (e.g., `AsyncLocal` wrapper, ambient context), prefer a DI-registered singleton over a static class. Static state should only be used for truly global constants (like `ActivitySource`). If the carrier is consumed by multiple services, it should be injectable so dependencies are explicit in constructors. |
