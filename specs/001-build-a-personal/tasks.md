# Tasks: Personal AI Assistant (Telegram Bot)

**Input**: IMPORTANT - Use design documents from `\specs\001-build-a-personal\`

## Phase 3.1: Setup
- [x] T001: Create projects `ChatBro.AppHost` and `ChatBro.ServiceDefaults` in `src/` folder. IMPORTANT: Use `dotnet new aspire -n ChatBro -o src` CLI command to create a project. Put solution `ChatBro.sln` file to the root directory. Add gitignore file for .NET projects. Ensure that solution can be built with `dotnet build`.
- [x] T002: Create project `ChatBro.TelegramBotService`. Register TelegramBotService in Aspire host. Use `ChatBro.ServiceDefaults` in `ChatBro.TelegramBotService` to apply common service defaults.
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