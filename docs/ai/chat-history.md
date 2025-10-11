# Chat History Implementation Plan

## Goal
- Enable the AI assistant to remember conversation context across multiple turns so replies reflect prior messages.
- Support simultaneous conversations for multiple users while keeping history in memory only (reset on service restart).

## Key Decisions
- Store per-user chat history in `IMemoryCache` within `ChatBro.AiService`, keyed by channel + user ID.
- Enforce sliding/absolute expiration and optional trimming to control memory usage; eviction under load is acceptable.
- Use Semantic Kernel's `ChatHistory` as the canonical structure for each session; wrap access with per-session locking for thread safety.
- Configure history behavior (TTL, max messages/tokens) via `Chat:History` options.

## General Idea
1. When a message arrives, the AI service resolves a cache entry for that user (creating it if missing), seeds the system context once, adds the new user message, and invokes the model with the accumulated history.
2. The assistant reply is appended to the same history before returning to the caller.
3. The Telegram bot (and other channels) include a stable user identifier in requests so each chat session is isolated.
4. Because the cache is in-memory, history naturally resets on service restarts or eviction.

---

## Phase 1 — Prototype with a single hardcoded user (`mock-user-id`)

1. **[DONE] Create project scaffolding**
   - Add `docs/ai/chat-history.md` (this plan) for reference.
2. **[DONE] Add configuration defaults**
   - Extend `appsettings.json` with `Chat:History` section (sliding TTL, absolute TTL, message/token limits).
   - Bind to a new `ChatHistoryOptions` class and register with options pattern.
3. **[DONE] Introduce history cache service**
   - Inject `IMemoryCache` into `ChatService`.
   - Implement private helper to `GetOrCreateSession("mock-user-id")` returning a session wrapper containing `ChatHistory`, timestamps, and a `SemaphoreSlim` lock.
   - Use configured cache entry options (`MemoryCacheEntryOptions`) when creating the session.
4. **Modify `ChatService.GetChatResponseAsync`**
   - Acquire session lock, append system context on first creation, then add user message before calling `GetChatMessageContentAsync`.
   - Append assistant response to history; update metadata (last activity, message count).
   - Apply trimming rules if limits exceeded (drop oldest user+assistant pairs until within bounds).
   - Release lock and return the response.
5. **Testing & validation**
   - Add unit tests for session creation/trimming using the hardcoded key.
   - Manual test via API (e.g., curl/Postman) sending sequential requests with implicit `mock-user-id`; confirm second response references previous message.
   - Document limitations (single conversation only) in README or issue tracker.

## Phase 2 — Real multi-user support via Telegram / channels

1. **Update API contracts**
   - Extend `ChatController` request DTO to include `userId` (string) and optional `channel` with default `telegram`.
   - Update validation and controller to pass `userId` to `ChatService`.
2. **Propagate identifiers through services**
   - Adjust `ChatService.GetChatResponseAsync` signature to accept `userId` (& optional channel) and use as cache key when calling history helpers.
   - Ensure existing callers (tests, other services) provide the new parameter.
3. **Telegram bot integration**
   - In `TelegramService`, derive `userId` from Telegram chat update (e.g., `$"telegram:{chat.Id}"`).
   - Update HTTP client calls to the AI service to include the derived `userId` field in the payload.
4. **Enhance caching helpers**
   - Replace hardcoded key with composite `<channel>:<userId>`.
   - Optionally expose helper for manual reset (e.g., slash command) if needed later.
5. **Concurrency & robustness**
   - Verify per-session locking works with multiple simultaneous chats (unit tests simulating parallel requests).
   - Ensure eviction scenarios are handled: on cache miss, new history is created seamlessly.
6. **Testing & observability**
   - Extend unit/integration tests for multiple user IDs to confirm histories stay isolated.
   - Manual end-to-end test via Telegram to confirm multi-turn conversations retain context.
   - Add logging/metrics for cache hits/misses if needed for monitoring.

## Nice-to-haves (post-MVP)
- Optional endpoint or bot command to clear a user’s history on demand.
- Surface history stats (count, age) via health check or diagnostics logs.
- Investigate persistence alternatives (e.g., distributed cache) if cross-instance scaling becomes necessary.
