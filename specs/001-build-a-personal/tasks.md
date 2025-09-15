# Tasks: Personal AI Assistant (Telegram Bot)

**Input**: Design documents from `D:\code\chat-bro\specs\001-build-a-personal\`

## Phase 3.1: Setup
- [x] T001: Create solution file `ChatBro.sln` in the root directory. Add gitignore file for .NET projects.
- [ ] T002: Create project `ChatBro.AspireHost` in `src/`. Use `aspire run` CLI command to create project.
- [ ] T003: Create project `ChatBro.TelegramBotService` in `src/`.
- [ ] T004: Create project `ChatBro.AIService` in `src/`.
- [ ] T005: Create project `ChatBro.RestaurantsService` in `src/`.
- [ ] T006: Create test project `ChatBro.End2EndTests` in `src/tests/`.
- [ ] T007: Add project references to `ChatBro.AspireHost`.

## Phase 3.2: "Hello World" Telegram Bot
- [ ] T008: [P] In `ChatBro.End2EndTests`, write a test that sends a message to the bot and expects a "Hello, World!" response.
- [ ] T009: In `ChatBro.TelegramBotService`, implement the basic bot logic to respond with "Hello, World!" to any message.

## Phase 3.3: AI-Powered Answering
- [ ] T010: [P] In `ChatBro.End2EndTests`, write a test that asks a simple question (e.g., "What is the capital of France?") and expects a correct answer.
- [ ] T011: In `ChatBro.AIService`, implement a service that uses Semantic Kernel to answer questions.
- [ ] T012: Integrate `ChatBro.AIService` with `ChatBro.TelegramBotService` to answer questions.

## Phase 3.4: Restaurant Recommendations
- [ ] T013: [P] In `ChatBro.End2EndTests`, write a test that asks for lunch recommendations and expects a list of restaurants.
- [ ] T014: In `ChatBro.RestaurantsService`, implement a service that fetches restaurant data from `lounaat.info`.
- [ ] T015: In `ChatBro.AIService`, create a Kernel Function to identify user intent for lunch recommendations.
- [ ] T016: Integrate the `RestaurantService` with the `AIService` to provide recommendations.

## Phase 3.5: Polish
- [ ] T017: [P] Add unit tests for all services.
- [ ] T018: [P] Implement structured logging and error handling in all services.

## Dependencies
- T001-T007 must be completed first.
- T008 (test) must be completed before T009 (implementation).
- T010 (test) must be completed before T011 and T012.
- T013 (test) must be completed before T014, T015, and T016.

## Parallel Example
```
# Launch T008, T010, and T013 together:
Task: "In `ChatBro.End2EndTests`, write a test that sends a message to the bot and expects a 'Hello, World!' response."
Task: "In `ChatBro.End2EndTests`, write a test that asks a simple question (e.g., 'What is the capital of France?') and expects a correct answer."
Task: "In `ChatBro.End2EndTests`, write a test that asks for lunch recommendations and expects a list of restaurants."
```

```