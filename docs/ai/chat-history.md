# Chat History Implementation Plan

## Goal
- Enable the AI assistant to remember conversation context across multiple turns so replies reflect prior messages.
- Support simultaneous conversations for multiple users while keeping history in memory only (reset on service restart).

## Key Decisions
- Store per-user chat history in `IMemoryCache` within `ChatBro.Server`, keyed by channel + user ID.
- Enforce sliding/absolute expiration and optional trimming to control memory usage; eviction under load is acceptable.
- Use Semantic Kernel's `ChatHistory` as the canonical structure for each session; wrap access with per-session locking for thread safety.
- Configure history behavior (TTL, max messages/tokens) via `Chat:History` options.

## General Idea
1. When a message arrives, the AI service resolves a cache entry for that user (creating it if missing), seeds the system context once, adds the new user message, and invokes the model with the accumulated history.
2. The assistant reply is appended to the same history before returning to the caller.
3. The Telegram bot (and other channels) include a stable user identifier in requests so each chat session is isolated.
4. Because the cache is in-memory, history naturally resets on service restarts or eviction.

