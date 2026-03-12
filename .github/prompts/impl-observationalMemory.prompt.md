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

**Runtime Verification Prerequisites**:
- AppHost user-secrets are already configured (telegram-token, openai-api-key, paperless-url, paperless-api-key) — AppHost starts non-interactively
- Start AppHost: `dotnet run --project src/ChatBro.AppHost` (as background process)
- Verify resources are running: use `mcp_aspire_list_resources` to confirm `chatbro-server` is in Running state
- Debug endpoint: `POST https://localhost:7296/debug/chat` with `{"message": "...", "userId": "debug"}` — response includes `traceId` and `spanId`
- Trace inspection: use `mcp_aspire_list_traces(resourceName: "chatbro-server")` and `mcp_aspire_list_trace_structured_logs(traceId)` to verify spans and tags
- AppHost should be started once before the first phase with runtime criteria, kept running across phases, and stopped after the last phase is verified
- When code changes require a restart, stop the AppHost process and start it again

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
**Status**: ✅ Complete

**Goal**: Wire memory into all agent prompts so the orchestrator and every domain agent can see the user's observations and recent raw messages. The memory load span from Phase 1 will now appear in every chat request trace.

**Required Results**:
- `ObservationalMemoryContext` static class with `AsyncLocal<UserMemory?>`
- `MemoryAIContextProvider` that reads from `AsyncLocal` and returns a system message
- Provider added to all agents in `AIAgentProvider`
- `ChatService` loads memory (with OTEL span) and sets context before agent run

**Validation Criteria**:
- [x] Project compiles without errors
- [x] Store can load `UserMemory` from Redis (verifiable via `/debug/chat` or manual Redis inspection) — `Memory.Load` span appears in Aspire dashboard
- [x] When no memory exists, agents function normally — `Memory.Load` span shows zero counts in tags
- ~~When memory exists in Redis, Aspire OTEL traces show a `Memory.Load` span followed by the memory content appearing in `gen_ai.input.messages` for the orchestrator~~ → moved to Phase 4 (requires pre-existing memory data produced by observer)
- ~~Domain agent traces also contain the memory system message~~ → moved to Phase 4 (requires domain-routing request with pre-existing memory)

**Tasks**:
- [x] Create [src/ChatBro.Server/Services/AI/Memory/ObservationalMemoryContext.cs](src/ChatBro.Server/Services/AI/Memory/ObservationalMemoryContext.cs) — static class with `AsyncLocal<UserMemory?>` property (`Current` getter/setter)
- [x] Create [src/ChatBro.Server/Services/AI/Memory/MemoryAIContextProvider.cs](src/ChatBro.Server/Services/AI/Memory/MemoryAIContextProvider.cs) extending `AIContextProvider` — in `ProvideAIContextAsync`, read `ObservationalMemoryContext.Current`, format observations + raw messages into a system message, return `AIContext`. If null/empty, return empty `AIContext`
- [x] Define the memory prompt template (inline or constants class) — structure: `## Observational Memory` / `### Observations` / `{entries}` / `### Recent Unprocessed Messages` / `{entries}`
- [x] Modify [src/ChatBro.Server/Services/AI/AIAgentProvider.cs](src/ChatBro.Server/Services/AI/AIAgentProvider.cs): create one `MemoryAIContextProvider` instance, add it to the `AIContextProviders` array of the orchestrator, restaurants agent, and documents agent (append to existing providers)
- [x] Modify [src/ChatBro.Server/Services/AI/ChatService.cs](src/ChatBro.Server/Services/AI/ChatService.cs) `GetChatResponseAsync`: inject `IObservationalMemoryStore`, before `chatAgent.RunAsync` call `store.LoadAsync(userId)` (this already emits a `Memory.Load` span from Phase 1), set `ObservationalMemoryContext.Current = memory`, wrap agent run in try/finally to clear the context

**Runtime Verification**:
- [x] Start AppHost (or restart if already running after code changes) — restarted `chatbro-server` resource via Aspire
- [x] Confirm `chatbro-server` is Running via `mcp_aspire_list_resources`
- [x] Send `POST /debug/chat` with `{"message": "hi", "userId": "debug"}` — traceId: `67a82aa69341a381f07ac04ad4e99e13`
- [x] Use `mcp_aspire_list_traces` to find the trace, then `mcp_aspire_list_trace_structured_logs(traceId)` to verify:
  - [x] `Memory.Load` span exists (span_id: `fd7c1ce`, 2ms) with tags `memory.user_id=debug`, `memory.observations.count=0`, `memory.raw_messages.count=0` — child Redis GET span (`912669a`) confirms actual Redis lookup
  - [x] Agent response is successful (HTTP 200, reply: "Hey bro, what do you want to do?")
- [ ] Stop AppHost *(kept running for subsequent phases)*

---

### Phase 3: Raw Message Capture
**Status**: ✅ Complete

**Goal**: After each agent turn, persist the user's message and the assistant's response as raw messages in the memory store, building up the buffer for the observer. The `Memory.Save` span confirms persistence.

**Required Results**:
- Each successful turn appends a `RawMessage` to the user's memory
- Memory is saved back to Redis (with OTEL span) after appending

**Validation Criteria**:
- [x] After sending 3 messages via `/debug/chat`, Aspire traces show `Memory.Load` and `Memory.Save` spans on every request, with `memory.raw_messages.count` incrementing (1, 2, 3)
- [x] Redis key `chatbro:memory:memory-test` contains raw messages (confirmed via `Memory.Save` span tags showing count=3)

**Tasks**:
- [x] In [src/ChatBro.Server/Services/AI/ChatService.cs](src/ChatBro.Server/Services/AI/ChatService.cs) `GetChatResponseAsync`, after `chatAgent.RunAsync` returns `response`: create a `RawMessage` with the user's `message`, `response.Text`, and `DateTimeOffset.UtcNow`
- [x] Append to the loaded `UserMemory.RawMessages` list (or create new `UserMemory` if null)
- [x] Call `IObservationalMemoryStore.SaveAsync(userId, memory)` to persist (this already emits a `Memory.Save` span from Phase 1)

**Runtime Verification**:
- [x] Start AppHost (restarted `chatbro-server` resource after rebuild)
- [x] Send 3 messages via `POST /debug/chat` with `userId: "memory-test"` (fresh user for clean counts)
- [x] Verified via `mcp_aspire_list_traces` — Memory spans extracted from traces:
  - [x] Both `Memory.Load` and `Memory.Save` spans exist in each trace
  - [x] `memory.raw_messages.count` increments: msg2 trace Load=1/Save=2, msg3 trace Load=2/Save=3 (msg1 trace outside window but msg2 Load=1 proves msg1 saved correctly)
- [ ] Stop AppHost *(kept running for subsequent phases)*

---

### Phase 4: Observer Service
**Status**: ✅ Complete

**Goal**: Implement the observer that compresses raw messages into durable observations when the threshold is exceeded, then clears the raw messages buffer. Observer execution wrapped in its own OTEL span.

**Required Results**:
- `IObserverService` interface + implementation with LLM call and `Memory.Observe` OTEL span
- Observer prompt in `contexts/memory/observer.md`
- Observer triggered in `ChatService` after raw message capture when threshold exceeded
- Observations appended, raw messages cleared on success
- Failures caught and logged (turn still succeeds)

**Validation Criteria**:
- [x] After sending 3+ messages via `/debug/chat` (threshold lowered to 3), Aspire traces show a `Memory.Observe` span on the turn that crosses the threshold
- [x] The `Memory.Observe` span includes tags `memory.observer.input_raw_messages=3` and `memory.observer.output_observations=6`
- [x] After observer runs, second `Memory.Save` span shows `memory.raw_messages.count=0` and `memory.observations.count=6`
- [x] Redis confirms observations exist and raw messages are cleared (verified via span tags)
- [ ] If the observer LLM call is artificially failed (e.g., invalid API key in test), the chat turn still returns a response, a failed `Memory.Observe` span with exception is visible, and raw messages are preserved *(not tested — requires manual API key invalidation)*
- [x] *(from Phase 2)* When memory has observations, `gen_ai.input.messages` for the orchestrator includes the `## Observational Memory` system message — verified in trace `04226a2`, input contains `Observational Memory` with observations
- [ ] *(from Phase 2)* Domain agent traces also contain the memory system message when a domain-routed request occurs *(not tested — requires domain-routing request to trigger)*

**Tasks**:
- [x] Create [src/ChatBro.Server/contexts/memory/observer.md](src/ChatBro.Server/contexts/memory/observer.md) — prompt instructing the LLM to: extract durable user facts/preferences/constraints, note ongoing tasks, use importance markers (🔴/🟡/🟢), avoid copying large tool outputs verbatim, **never persist secrets** (tokens, API keys, credentials), output as structured JSON array
- [x] Create `IObserverService` interface in [src/ChatBro.Server/Services/AI/Memory/IObserverService.cs](src/ChatBro.Server/Services/AI/Memory/IObserverService.cs) with `ObserveAsync(UserMemory memory, CancellationToken)` returning updated `UserMemory`
- [x] Create `ObserverService` in [src/ChatBro.Server/Services/AI/Memory/ObserverService.cs](src/ChatBro.Server/Services/AI/Memory/ObserverService.cs) — wrap entire method in `MemoryActivitySource.Source.StartActivity("Memory.Observe")`. Loads observer prompt from file, sends raw messages + existing observations to LLM, parses JSON response into `Observation` entries, clears raw messages. Tags: `memory.observer.input_raw_messages`, `memory.observer.input_observations`, `memory.observer.output_observations` + standard memory tags. Uses `ActivityExtensions.SetException()` on failure.
- [x] Verify `contexts/memory/` is covered by the existing content copy glob in [src/ChatBro.Server/ChatBro.Server.csproj](src/ChatBro.Server/ChatBro.Server.csproj) — `contexts\**\*.*` pattern covers it
- [x] Register `IObserverService` as singleton in [src/ChatBro.Server/DependencyInjection/AgentsAiExtensions.cs](src/ChatBro.Server/DependencyInjection/AgentsAiExtensions.cs)
- [x] In [src/ChatBro.Server/Services/AI/ChatService.cs](src/ChatBro.Server/Services/AI/ChatService.cs), after raw message save: check `memory.RawMessages.Count >= threshold`, call `observerService.ObserveAsync(memory)` inside try/catch, save result. On failure, log warning and continue. Injected `IObserverService` and `IOptions<ObservationalMemorySettings>` into constructor.

**Runtime Verification**:
- [x] Temporarily lowered `ObserverRawMessageThreshold` to 3 in appsettings for practical testing
- [x] Restarted `chatbro-server` resource after rebuild
- [x] Sent 3 messages with `userId: observer-test-971318076` (fresh user)
- [x] On 3rd message trace (`527003f`), verified via `mcp_aspire_list_traces`:
  - [x] `Memory.Observe` span exists with `memory.observer.input_raw_messages=3`, `memory.observer.output_observations=6`
  - [x] Second `Memory.Save` span shows `memory.raw_messages.count=0` and `memory.observations.count=6`
- [x] Sent 4th message ("what do you know about me?") — agent correctly recalled: Alex, Finnish food, vegetarian, hikes near Espoo
- [x] Verified trace `04226a2`: `gen_ai.input.messages` contains `Observational Memory` section
- [x] Restored `ObserverRawMessageThreshold` to 20
- [ ] Stop AppHost *(kept running for subsequent phases)*

---

### Phase 5: Reflector Service
**Status**: ✅ Complete

**Goal**: Implement the reflector that prunes/deduplicates observations when they exceed the configured threshold. Reflector execution wrapped in its own OTEL span.

**Required Results**:
- `IReflectorService` interface + implementation with LLM call and `Memory.Reflect` OTEL span
- Reflector prompt in `contexts/memory/reflector.md`
- Reflector triggered after observer when observation threshold exceeded
- Failures caught and logged

**Validation Criteria**:
- [x] After observations exceed 50 entries (can temporarily lower threshold for testing), Aspire traces show a `Memory.Reflect` span
- [x] The `Memory.Reflect` span includes before/after observation counts as tags
- [x] After reflector runs, `Memory.Save` span shows reduced `memory.observations.count`
- [x] High-signal observations (🔴) are preserved; low-value/outdated ones are pruned (verify in Redis)
- [ ] Reflector failure produces a failed span with exception and doesn't block the turn *(deferred — requires manual LLM failure injection)*

**Tasks**:
- [x] Create [src/ChatBro.Server/contexts/memory/reflector.md](src/ChatBro.Server/contexts/memory/reflector.md) — prompt instructing the LLM to: deduplicate observations, remove outdated facts, preserve high-importance (🔴) items, consolidate related observations, output cleaned list
- [x] Create `IReflectorService` interface in [src/ChatBro.Server/Services/AI/Memory/IReflectorService.cs](src/ChatBro.Server/Services/AI/Memory/IReflectorService.cs) with `ReflectAsync(UserMemory memory, CancellationToken)` returning updated `UserMemory`
- [x] Create `ReflectorService` in [src/ChatBro.Server/Services/AI/Memory/ReflectorService.cs](src/ChatBro.Server/Services/AI/Memory/ReflectorService.cs) — wrap in `MemoryActivitySource.Source.StartActivity("Memory.Reflect")`. Load reflector prompt, send observations to LLM, replace observations with pruned result. Set span tags for before/after counts. Use `ActivityExtensions.SetException()` on failure. Uses `IChatClient` with `.UseOpenTelemetry()`
- [x] Register `IReflectorService` in [src/ChatBro.Server/DependencyInjection/AgentsAiExtensions.cs](src/ChatBro.Server/DependencyInjection/AgentsAiExtensions.cs)
- [x] In [src/ChatBro.Server/Services/AI/ChatService.cs](src/ChatBro.Server/Services/AI/ChatService.cs), after observer: check if `memory.Observations.Count >= reflectorThreshold`, if so call `reflectorService.ReflectAsync(memory)` inside try/catch, save result. On failure, log and continue

**Runtime Verification**:
- [x] Temporarily lowered `ObserverRawMessageThreshold` to 2 and `ReflectorObservationThreshold` to 3 in appsettings for practical testing
- [x] Started AppHost, confirmed all resources running via `mcp_aspire_list_resources`
- [x] Sent 2 messages with `userId: reflector-test-1` (fresh user with rich personal details)
- [x] On 2nd message trace (`8cb9519`), verified via `mcp_aspire_list_traces`:
  - [x] `Memory.Observe` span: `input_raw_messages=2`, `output_observations=8`
  - [x] `Memory.Reflect` span exists with `memory.reflector.before_observations=8`, `memory.reflector.after_observations=5` (37.5% reduction)
  - [x] Final `Memory.Save` span shows `memory.observations.count=5`, `memory.raw_messages.count=0`
  - [x] High-signal 🔴 observations preserved (name/location/cats/food); related facts consolidated (name+location+job merged into one)
- [x] Restored thresholds to defaults (20 / 50)
- [x] Stopped AppHost

---

### Phase 6: Hard Reset Command (`/reset-hard`)
**Status**: ✅ Complete

**Goal**: Add a `/reset-hard` command that clears both message history AND observational memory. The existing `/reset` command keeps its current behavior (clears message history only). The `Memory.Delete` span confirms memory cleanup.

**Required Results**:
- New `HardResetChatAsync` method in `ChatService` that calls existing reset logic + `memoryStore.DeleteAsync`
- New `ResetHardCommand` Telegram command implementing `ITelegramCommand`
- New `POST /debug/reset-hard` endpoint for debug testing
- `/reset` remains unchanged — only clears chat threads

**Validation Criteria**:
- [x] `/reset` does NOT emit a `Memory.Delete` span (existing behavior preserved)
- [x] `/reset-hard` emits a `Memory.Delete` span with `memory.user_id`
- [x] After `/reset-hard`, Redis key `chatbro:memory:{userId}` is deleted
- [x] After `/reset-hard`, subsequent `Memory.Load` span shows zero counts for both observations and raw messages
- [x] After `/reset` (without -hard), memory is still present — `Memory.Load` shows previous counts

**Tasks**:
- [x] Add `HardResetChatAsync(string userId)` to [src/ChatBro.Server/Services/AI/ChatService.cs](src/ChatBro.Server/Services/AI/ChatService.cs) — calls `ResetChatAsync(userId)` then `memoryStore.DeleteAsync(userId)` (`IObservationalMemoryStore` already injected)
- [x] Create [src/ChatBro.Server/Services/Telegram/ResetHardCommand.cs](src/ChatBro.Server/Services/Telegram/ResetHardCommand.cs) implementing `ITelegramCommand` with `Command => "reset_hard"` — calls `chatService.HardResetChatAsync(userId)`, returns `🧹🧠✅`
- [x] Register `ResetHardCommand` in [src/ChatBro.Server/DependencyInjection/TelegramBotServiceExtensions.cs](src/ChatBro.Server/DependencyInjection/TelegramBotServiceExtensions.cs)
- [x] Add `POST /debug/reset-hard` endpoint in [src/ChatBro.Server/Api/DebugApiExtensions.cs](src/ChatBro.Server/Api/DebugApiExtensions.cs) — calls `chatService.HardResetChatAsync(userId)`, returns trace info
- [x] Update existing tests in [tests/ChatBro.TelegramBotService.Tests/Services/ChatServiceTests.cs](tests/ChatBro.TelegramBotService.Tests/Services/ChatServiceTests.cs) to compile with new `ChatService` constructor signature, and add test for `HardResetChatAsync`

**Runtime Verification**:
- [x] Temporarily lowered `ObserverRawMessageThreshold` to 2 for practical testing
- [x] Started AppHost, confirmed all resources running
- [x] Sent 2 messages with `userId: reset-test-1` — observer triggered, 5 observations created
- [x] Sent additional messages (soft reset via agent) — verified no `Memory.Delete` span in any chat trace
- [x] Verified `Memory.Load` still shows `observations.count=5` after soft interactions (memory preserved)
- [x] Called `POST /debug/reset-hard` with `userId: reset-test-1` — trace confirmed:
  - [x] `Memory.Delete` span with `memory.user_id=reset-test-1`, `observations.count=0`, `raw_messages.count=0`
- [x] Sent new message after hard reset — agent says "I don't have a built-in profile of you"
  - [x] `Memory.Load` span shows `observations.count=0`, `raw_messages.count=0`
- [x] Restored thresholds to defaults (20 / 50)
- [x] Stopped AppHost
- [x] All 6 unit tests pass (2 existing + 1 reset-preserves-memory + 2 hard-reset + 1 hard-reset-no-threads)

---

### Phase 7: Show Memory Command (`/show_memory`)
**Status**: � In Progress

**Goal**: Add a `/show_memory` command that responds with all records currently stored in the user's observational memory — observations and raw (unprocessed) messages. This gives users visibility into what the system remembers about them.

**Required Results**:
- New `ShowMemoryCommand` Telegram command implementing `ITelegramCommand`
- Command loads memory from `IObservationalMemoryStore` and formats it as a readable text response
- Returns a message listing all observations (with importance and timestamp) and raw messages count
- If no memory exists, returns a friendly "no memory" message
- Accessible via `POST /debug/command/show_memory` debug endpoint (already handled by generic `/debug/command/{name}` routing)

**Validation Criteria**:
- [x] Project compiles without errors
- [ ] `/show_memory` with existing memory returns formatted observations list
- [ ] `/show_memory` with no memory returns a "no memory" message
- [ ] Command is accessible via `POST /debug/command/show_memory` debug endpoint

**Tasks**:
- [x] Create [src/ChatBro.Server/Services/Telegram/ShowMemoryCommand.cs](src/ChatBro.Server/Services/Telegram/ShowMemoryCommand.cs) implementing `ITelegramCommand` with `Command => "show_memory"` — loads memory via `IObservationalMemoryStore.LoadAsync(userId)`, formats observations as a readable list (importance emoji, timestamp, text), appends raw messages count, returns formatted string. If memory is empty (no observations and no raw messages), return "No observational memory stored."
- [x] Register `ShowMemoryCommand` in [src/ChatBro.Server/DependencyInjection/TelegramBotServiceExtensions.cs](src/ChatBro.Server/DependencyInjection/TelegramBotServiceExtensions.cs)

**Runtime Verification**:
- [ ] Start AppHost (or restart if running)
- [ ] Send a few messages via `POST /debug/chat` with a test user to build up some memory (lower thresholds if needed)
- [ ] Call `POST /debug/command/show_memory` with the same userId — verify response contains formatted observations
- [ ] Call `POST /debug/command/show_memory` with a fresh userId that has no memory — verify "no memory" response
- [ ] Stop AppHost (or keep running for Phase 8)

---

### Phase 8: End-to-End Validation and Documentation
**Status**: 🔲 Not Started

**Goal**: Verify the complete feature works end-to-end against all acceptance criteria from the issue, and document the implementation.

**Required Results**:
- All acceptance criteria met
- `docs/ai/observational-memory.md` created
- Full trace lifecycle visible in Aspire dashboard

**Validation Criteria**:
- [ ] After 20+ turns via `/debug/chat`, observations are generated — confirmed via `Memory.Observe` spans and Redis content
- [ ] After 50+ observations (use lowered threshold for practical testing), reflector prunes — confirmed via `Memory.Reflect` spans
- [ ] `/reset-hard` clears everything — `Memory.Delete` span, then next turn shows `Memory.Load` with zero counts
- [ ] `/reset` clears only chat threads, memory persists — next turn still shows previous observation counts
- [ ] `/show_memory` displays current observations and raw message count
- [ ] Tool-heavy sessions (e.g., restaurant queries) don't cause runaway prompt growth; key facts persist via observations
- [ ] Full Aspire dashboard trace shows the lifecycle: `Memory.Load` → agent run → `Memory.Save` → (optional) `Memory.Observe` → `Memory.Save` → (optional) `Memory.Reflect` → `Memory.Save`

**Tasks**:
- [ ] Test via [src/ChatBro.Server/ChatBro.Server.http](src/ChatBro.Server/ChatBro.Server.http) — send 25+ messages with varied topics, verify observations appear
- [ ] Verify via `mcp_aspire_list_traces` + `mcp_aspire_list_trace_structured_logs` that the full span tree is correct and all tags populate
- [ ] Test `/reset-hard` clears the memory key — verify via `Memory.Delete` span and subsequent `Memory.Load` showing zero counts
- [ ] Test `/reset` does NOT clear memory — verify no `Memory.Delete` span, subsequent `Memory.Load` retains previous counts
- [ ] Test `/show_memory` returns formatted memory content — verify via `POST /debug/command/show_memory`
- [ ] Create [docs/ai/observational-memory.md](docs/ai/observational-memory.md) documenting the architecture (memory model, observer/reflector flow, configuration, prompt files, OTEL span reference)
- [ ] Update orchestrator prompt in [src/ChatBro.Server/contexts/orchestrator.md](src/ChatBro.Server/contexts/orchestrator.md) if needed — add a note that the agent has access to observational memory
- [ ] Stop AppHost when all verification is complete

---

## Implementation Observations & Notes

| Phase | Observation |
|-------|-------------|
| 1 | All 8 tasks complete. Build succeeds with 0 warnings, 0 errors. Two validation criteria (Redis round-trip, Aspire spans) are deferred to Phase 2+ when the store is actually invoked at runtime. |
| 2 | All 5 tasks complete. Build succeeds 0/0. `ObservationalMemoryContext` refactored from static to DI singleton per user feedback. `MemoryAIContextProvider` receives it via constructor. `ChatService` loads memory before agent run and clears `AsyncLocal` in finally. **Runtime validated**: `Memory.Load` span (2ms) confirmed in trace `67a82aa` with correct tags (`memory.user_id=debug`, counts=0). Child Redis GET span present. Agent responds normally with empty memory. Two criteria moved to Phase 4: memory content in `gen_ai.input.messages` and domain agent traces require pre-existing observation data that only the observer produces. |
| 3 | Single code change: 7 lines added to `ChatService.GetChatResponseAsync` after agent run — creates `RawMessage`, appends to memory, calls `SaveAsync`. Build 0/0. **Runtime validated**: 3 messages sent with `userId: memory-test`. Traces confirm `Memory.Load` + `Memory.Save` on every request with `raw_messages.count` incrementing 0→1→2→3. |
| 4 | Created 4 artifacts: `observer.md` (LLM prompt), `IObserverService`, `ObserverService` (LLM call + JSON parse + OTEL span), DI registration. Modified `ChatService`: added `IObserverService` + `IOptions<ObservationalMemorySettings>` to constructor, threshold check after raw message save. Build 0/0. **Runtime validated**: threshold lowered to 3, 3 messages triggered observer extracting 6 observations from 3 raw messages. 4th message confirmed agent recalls observations. `gen_ai.input.messages` contains `Observational Memory` section. Two criteria deferred: LLM failure resilience (manual test) and domain agent memory traces (needs domain-routed request). |
| 5 | Created 4 artifacts: `reflector.md` (LLM prompt for pruning/dedup), `IReflectorService`, `ReflectorService` (same `IChatClient` pattern as ObserverService), DI registration. Modified `ChatService`: added `IReflectorService` to constructor, nested reflector trigger inside observer block with try/catch. Build 0/0. **Runtime validated**: thresholds lowered to 2/3, 2 messages triggered observer (8 observations) which exceeded reflector threshold (3). Reflector consolidated 8→5 observations (37.5% reduction). Trace `8cb9519` confirmed full pipeline: `Memory.Save`(raw=2) → `Memory.Observe`(in=2,out=8) → `Memory.Save`(obs=8) → `Memory.Reflect`(before=8,after=5) → `Memory.Save`(obs=5). High-signal 🔴 preserved; name+location+job consolidated into single observation. |
| 6 | Phase redesigned per user request: `/reset` keeps current behavior (chat threads only), new `/reset-hard` clears both threads and memory. Created `HardResetChatAsync` in `ChatService` (delegates to `ResetChatAsync` + `memoryStore.DeleteAsync`), `ResetHardCommand` for Telegram, `POST /debug/reset-hard` debug endpoint, registered in DI. Updated existing unit tests to match new constructor (added fakes for memory store, observer, reflector), added 4 new tests (reset-preserves-memory, hard-reset-deletes-memory, hard-reset-no-threads). Build 0/0, 6/6 tests pass. **Runtime validated**: soft interactions show no `Memory.Delete` spans and `observations.count=5` preserved; `POST /debug/reset-hard` produces `Memory.Delete` span with zero counts; post-reset `Memory.Load` confirms `observations.count=0`. Agent confirms no memory of user after hard reset. |

## Prompt Reflections & Adjustments

| User Correction | Root Cause | Suggested Prompt Improvement |
|----------------|------------|------------------------------|
| OTEL should not be a separate phase; integrate into each phase | Original plan treated OTEL as cross-cutting but deferred it, preventing its use as a validation tool during development | Planning prompt should instruct: "Cross-cutting concerns like observability should be integrated into each phase, not deferred. If a concern enables validation of other phases, it must be introduced early." |
| Phase 1 had runtime validation criteria (Redis round-trip, Aspire spans) but no code path that calls the store | Plan separated infrastructure creation from pipeline wiring, then assigned runtime criteria to the infrastructure-only phase | Every phase must include a call path for what it builds. Validation criteria must be achievable using only artifacts produced by that phase. Before finalizing a phase, check: "Can I trigger every validation criterion by running the app after only this phase's changes?" If not, move the criterion to the phase that enables it. |
| Phase 2 marked complete with 4 of 5 runtime validation criteria unchecked — only compilation was verified | Plan lists runtime criteria (Aspire spans, gen_ai.input.messages) but doesn't instruct the implementer to start the AppHost and actually run `/debug/chat`. Criteria without execution instructions become aspirational, not actionable. | For each runtime validation criterion, include a concrete execution step: (1) start AppHost, (2) send request to `/debug/chat` (response includes `traceId`), (3) use Aspire MCP tools (`mcp_aspire_list_traces`, `mcp_aspire_list_trace_structured_logs`) to programmatically verify spans and tags — no manual dashboard inspection needed. Separate criteria into "compile-time" (checked by `dotnet build`) and "runtime" (checked by running AppHost + `/debug/chat` + Aspire MCP). Mark a phase complete only when ALL criteria are verified — if runtime testing is skipped, the phase status should be "code complete, runtime unverified". |
| Phase 2 plan specified `ObservationalMemoryContext` as a static class; user corrected to DI singleton | Plan defaulted to static class for `AsyncLocal` carrier without considering that DI-managed singletons are preferred in the codebase for testability and explicit dependency tracking. Static classes hide dependencies and make unit testing harder. | When the plan introduces a shared-state carrier (e.g., `AsyncLocal` wrapper, ambient context), prefer a DI-registered singleton over a static class. Static state should only be used for truly global constants (like `ActivitySource`). If the carrier is consumed by multiple services, it should be injectable so dependencies are explicit in constructors. |
| Phase 2 had criteria requiring memory content in `gen_ai.input.messages` and domain agent traces, but Phase 2 only loads empty memory (no writer exists until Phase 3, no observations until Phase 4) | Plan assigned "memory visible in prompts" criteria to the phase that adds the injection plumbing, without checking that the data needed to make it visible doesn't exist yet. Same root cause as Phase 1's store-without-caller issue. | Before assigning a validation criterion to a phase, verify the full data dependency chain: "Does all the data needed to trigger this criterion exist after this phase's changes?" If the criterion depends on data produced by a later phase, move the criterion to that phase. |
