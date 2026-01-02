# ChatBro AI Coding Instructions

## Architecture Overview

ChatBro is a **multi-agent AI assistant** delivered via Telegram, built on **.NET 10 Aspire** with a hierarchical agent architecture.

### Service Structure

- **ChatBro.AppHost** - .NET Aspire orchestrator managing all services, parameters, and dependencies
- **ChatBro.Server** - Main service hosting the Telegram bot and orchestrator AI agent
- **ChatBro.RestaurantsService** - Standalone microservice scraping Finnish restaurant lunch menus
- **ChatBro.ServiceDefaults** - Shared configuration for health checks, OpenTelemetry, and service discovery

### Multi-Agent Pattern

The system uses a **two-tier agent architecture**:

1. **Orchestrator Agent** ([orchestrator.md](../src/ChatBro.Server/contexts/orchestrator.md)) - Routes user requests to domain agents using tools
2. **Domain Agents** (under `contexts/domains/`) - Handle specific domains (restaurants, documents) with their own context and tools

**Key Implementation**: See [AIAgentProvider.cs](../src/ChatBro.Server/Services/AI/AIAgentProvider.cs) - domain agents are exposed as `AITool` functions to the orchestrator via `AsAIFunction()`. Each domain maintains separate conversation threads per user (format: `userId::domainKey`).

**Adding a Domain Agent**:
1. Create `contexts/domains/{domain}/` with `description.md` and `instructions.md`
2. Add domain to `ChatSettings.Domains` in appsettings
3. Register in `AIAgentProvider.BuildDomainAgentsAsync()` - create agent, wrap as tool, return `DomainAgentRegistration`
4. Update `DomainToolingBuilder` to handle thread management

## Critical Patterns

### AI Function Plugins (Semantic Kernel Style)

Functions exposed to AI agents follow this pattern (see [RestaurantsPlugin.cs](../src/ChatBro.RestaurantsService.KernelFunction/RestaurantsPlugin.cs)):

```csharp
[Description("Detailed description for the AI model")]
public static async Task<string> FunctionName(
    [Description("Parameter description")] Type param,
    IServiceProvider serviceProvider)
{
    var service = serviceProvider.GetRequiredService<ServiceType>();
    // Implementation
}
```

### Aspire Configuration

**Parameter Management**: UI parameters in `AppHost.cs` use `CreateUiParameter()` helper:
- Secret parameters (tokens, API keys): `InputType.SecretText`
- Check `builder.Configuration` first - if value exists in config, skip interactive input
- Non-secret parameters: `InputType.Text`

**Service References**: Use `WithReference()` for service-to-service communication:
```csharp
builder.AddProject<ChatBro_Server>("chatbro-server")
    .WithReference(redis).WaitFor(redis)
    .WithReference(restaurants).WaitFor(restaurants)
```

**Custom Resources**: For non-standard services, extend `ContainerResource` and implement `IResourceWithConnectionString` - see [PaperlessMcpExtensions.cs](../src/ChatBro.AppHost/PaperlessMcpExtensions.cs) for MCP server integration pattern.

### HTTP Client Configuration

**Service Discovery**: Named clients automatically resolve via Aspire service discovery:
```csharp
builder.Services.AddHttpClient<RestaurantsServiceClient>(
    static client => client.BaseAddress = new Uri("https+http://chatbro-restaurants"));
```

**Timeouts**: Default HTTP timeout extended to 10 minutes in [Extensions.cs](../src/ChatBro.ServiceDefaults/Extensions.cs#L32) for AI agent operations. For custom needs, create specialized `IHttpClientFactory` configurations.

## Development Workflows

### Running the Application

```powershell
# Run via Aspire (recommended - orchestrates all services)
cd src/ChatBro.AppHost
dotnet run

# Access Aspire Dashboard at https://localhost:17239 (or as displayed)
# Interactive parameters will prompt for: telegram-token, openai-api-key, paperless-url, paperless-api-key
```

### Running Tests

```powershell
# Run all tests
dotnet test

# Run specific integration test
dotnet test tests/ChatBro.IntegrationTests --filter "FullyQualifiedName~TelegramBotService"
```

## Project-Specific Conventions

### Naming
- Services: `ChatBro.{ServiceName}` (e.g., `ChatBro.RestaurantsService`)
- Kernel plugins: `{Domain}Plugin` with static methods exposed to AI
- Domain agents: `{Domain}Agent` as variable names, threads use `domainKey` from config

### File Organization
- AI context files: `contexts/` folder structure with markdown files
- Domain definitions: `contexts/domains/{domain}/description.md` and `instructions.md`
- Options: Dedicated `Options/` folder with `{Feature}Options.cs` classes validated via `ValidateDataAnnotations()`

### Dependencies
- **Microsoft.Agents.AI** - Agent orchestration framework
- **Microsoft.Extensions.AI** - AI abstraction layer for tools and function calling
- **OpenAI .NET SDK** - Direct OpenAI client integration
- **Aspire.Hosting** - Service orchestration and discovery

### Configuration
- Aspire connection strings injected via `builder.Configuration.GetConnectionString()`
- Service URLs resolve via service discovery (no hardcoded URLs)
- All secrets managed through Aspire parameters or user secrets

## Common Tasks

**Add new AI function to domain agent**: Create static method with `[Description]` in plugin class, register in agent's tools list in `AIAgentProvider`

**Modify agent behavior**: Edit `instructions.md` in `contexts/domains/{domain}/` - changes apply immediately on next agent initialization

**Add new service**: Create project under `src/`, add to AppHost with `builder.AddProject<>()`, reference `ChatBro.ServiceDefaults`
