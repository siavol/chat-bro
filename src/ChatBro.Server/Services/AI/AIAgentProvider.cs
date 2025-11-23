using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ChatBro.Server.Options;
using ChatBro.RestaurantsService.KernelFunction;
using ChatBro.Server.Services.AI.Plugins;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI;

namespace ChatBro.Server.Services.AI;

#pragma warning disable MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

public sealed class AIAgentProvider(
    IOptions<ChatSettings> chatSettings,
    OpenAIClient openAiClient,
    FunctionMiddleware functionMiddleware,
    PaperlessMcpClient paperlessMcpClient,
    IServiceProvider serviceProvider,
    ILogger<AIAgentProvider> logger) : IAIAgentProvider, IDisposable
{
    private readonly ChatSettings _chatSettings = chatSettings.Value;
    private readonly OpenAIClient _openAiClient = openAiClient;
    private readonly FunctionMiddleware _functionMiddleware = functionMiddleware;
    private readonly PaperlessMcpClient _paperlessMcpClient = paperlessMcpClient;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<AIAgentProvider> _logger = logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private AIAgent? _orchestratorAgent;
    private IReadOnlyList<DomainAgentRegistration>? _domainAgents;

    public async Task<AIAgent> GetAgentAsync()
    {
        await EnsureInitializedAsync();
        return _orchestratorAgent!;
    }

    public async Task<IReadOnlyList<DomainAgentRegistration>> GetDomainAgentsAsync(CancellationToken cancellationToken = default)
    {
        _ = cancellationToken;
        await EnsureInitializedAsync();
        return _domainAgents!;
    }

    private async Task EnsureInitializedAsync()
    {
        if (_orchestratorAgent != null && _domainAgents != null)
        {
            return;
        }

        await _lock.WaitAsync();
        try
        {
            if (_orchestratorAgent != null && _domainAgents != null)
            {
                return;
            }

            _logger.LogInformation("Building orchestrator and domain agents");
            var domainAgents = await BuildDomainAgentsAsync();
            _domainAgents = domainAgents;
            _orchestratorAgent = BuildOrchestratorAgent();

            _logger.LogInformation(
                "Constructed orchestrator agent plus {Count} domain agents",
                domainAgents.Count);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<IReadOnlyList<DomainAgentRegistration>> BuildDomainAgentsAsync()
    {
        var registrations = new List<DomainAgentRegistration>(capacity: 2)
        {
            await BuildRestaurantsDomainAgentAsync(),
            await BuildDocumentsDomainAgentAsync()
        };

        return registrations;
    }

    private Task<DomainAgentRegistration> BuildRestaurantsDomainAgentAsync()
    {
        var domainSettings = _chatSettings.Domains.Restaurants;
        var tools = new List<AITool>
        {
            AIFunctionFactory.Create(RestaurantsPlugin.GetRestaurants, name: "get_restaurants"),
            AIFunctionFactory.Create(DateTimePlugin.CurrentDateTime, name: "get_current_datetime")
        };

        var agent = CreateAgent(
            name: "ChatBro.RestaurantsAgent",
            description: "Handles restaurant discovery and lunch planning.",
            instructionsPath: domainSettings.Instructions,
            tools: tools,
            telemetrySource: "ChatBro.Server.Agent.Restaurants");

        var registration = CreateRegistration(
            domainSettings,
            defaultKey: "restaurants",
            defaultToolName: "restaurants_chat",
            agent);

        return Task.FromResult(registration);
    }

    private async Task<DomainAgentRegistration> BuildDocumentsDomainAgentAsync()
    {
        var domainSettings = _chatSettings.Domains.Documents;
        var tools = await _paperlessMcpClient.GetToolsAsync();

        var agent = CreateAgent(
            name: "ChatBro.DocumentsAgent",
            description: "Looks up and files Paperless documents.",
            instructionsPath: domainSettings.Instructions,
            tools: tools,
            telemetrySource: "ChatBro.Server.Agent.Documents");

        return CreateRegistration(
            domainSettings,
            defaultKey: "documents",
            defaultToolName: "documents_chat",
            agent);
    }

    private DomainAgentRegistration CreateRegistration(
        ChatSettings.DomainSettings settings,
        string defaultKey,
        string defaultToolName,
        AIAgent agent)
    {
        var key = string.IsNullOrWhiteSpace(settings.Key) ? defaultKey : settings.Key;
        var toolName = string.IsNullOrWhiteSpace(settings.ToolName) ? defaultToolName : settings.ToolName;
        var description = string.IsNullOrWhiteSpace(settings.Description)
            ? $"Routes conversations to {agent.Name}."
            : settings.Description;

        return new DomainAgentRegistration(key, toolName, description, agent);
    }

    private AIAgent BuildOrchestratorAgent()
    {
        var orchestrator = _chatSettings.Orchestrator;
        var tools = new List<AITool>
        {
            AIFunctionFactory.Create(DateTimePlugin.CurrentDateTime, name: "get_current_datetime")
        };

        var name = string.IsNullOrWhiteSpace(orchestrator.Name)
            ? "ChatBro Orchestrator"
            : orchestrator.Name;
        var description = string.IsNullOrWhiteSpace(orchestrator.Description)
            ? "Routes user questions to the best domain expert."
            : orchestrator.Description;

        return CreateAgent(
            name,
            description,
            orchestrator.Instructions,
            tools,
            telemetrySource: "ChatBro.Server.Agent.Orchestrator");
    }

    private AIAgent CreateAgent(
        string name,
        string description,
        string instructionsPath,
        IEnumerable<AITool> tools,
        string telemetrySource)
    {
        if (string.IsNullOrWhiteSpace(instructionsPath))
        {
            throw new InvalidOperationException($"Instructions path is not configured for agent {name}.");
        }

        var options = new ChatClientAgentOptions
        {
            Name = name,
            Description = description,
            AIContextProviderFactory = _ => CreateContextProvider(instructionsPath),
            ChatOptions = new ChatOptions
            {
                Tools = tools.ToArray()
            },
            ChatMessageStoreFactory = ctx => new InMemoryChatMessageStore(
                new MessageCountingChatReducer(_chatSettings.History.ReduceOnMessageCount),
                ctx.SerializedState,
                ctx.JsonSerializerOptions,
                InMemoryChatMessageStore.ChatReducerTriggerEvent.AfterMessageAdded)
        };

        return _openAiClient
            .GetChatClient(_chatSettings.AiModel)
            .CreateAIAgent(options, services: _serviceProvider)
            .AsBuilder()
            .Use(_functionMiddleware.CustomFunctionCallingMiddleware)
            .UseOpenTelemetry(
                sourceName: telemetrySource,
                configure: cfg => cfg.EnableSensitiveData = true)
            .Build();
    }

    private FileBackedAIContextProvider CreateContextProvider(string instructionsPath)
        => ActivatorUtilities.CreateInstance<FileBackedAIContextProvider>(_serviceProvider, instructionsPath);

    public void Dispose()
    {
        _lock.Dispose();
    }
}

