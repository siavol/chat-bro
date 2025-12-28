using ChatBro.Server.Options;
using ChatBro.RestaurantsService.KernelFunction;
using ChatBro.Server.Services.AI.Plugins;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
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
            name: "RestaurantsAgent",
            description: domainSettings.Description,
            agentKey: domainSettings.Key,
            tools: tools,
            contextProviderFactory: () => CreateDomainAgentContextProvider(domainSettings.Key));

        var registration = DomainAgentRegistration.Create(domainSettings, agent);

        return Task.FromResult(registration);
    }

    private async Task<DomainAgentRegistration> BuildDocumentsDomainAgentAsync()
    {
        var domainSettings = _chatSettings.Domains.Documents;
        var tools = await _paperlessMcpClient.GetToolsAsync();

        var agent = CreateAgent(
            name: "DocumentsAgent",
            description: domainSettings.Description,
            agentKey: domainSettings.Key,
            tools: tools,
            contextProviderFactory: () => CreateDomainAgentContextProvider(domainSettings.Key));

        return DomainAgentRegistration.Create(domainSettings, agent);
    }

    private AIAgent BuildOrchestratorAgent()
    {
        var orchestrator = _chatSettings.Orchestrator;
        var tools = new List<AITool>
        {
            AIFunctionFactory.Create(DateTimePlugin.CurrentDateTime, name: "get_current_datetime")
        };

        return CreateAgent(
            "OrchestratorAgent",
            orchestrator.Description,
            orchestrator.Instructions,
            tools,
            contextProviderFactory: () => CreateOrchestratorContextProvider());
    }

    private AIAgent CreateAgent(
        string name,
        string description,
        string agentKey,
        IEnumerable<AITool> tools,
        Func<AIContextProvider> contextProviderFactory)
    {
        if (string.IsNullOrWhiteSpace(agentKey))
        {
            throw new InvalidOperationException($"Agent key is not configured for agent {name}.");
        }

        var telemetrySource = $"ChatBro.Server.Agent.{name}";

        var options = new ChatClientAgentOptions
        {
            Name = name,
            Description = description,
            AIContextProviderFactory = _ => contextProviderFactory(),
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

    private DomainAgentAIContextProvider CreateDomainAgentContextProvider(string agentKey)
        => ActivatorUtilities.CreateInstance<DomainAgentAIContextProvider>(_serviceProvider, agentKey);
    
    private OrchestratorAIContextProvider CreateOrchestratorContextProvider()
        => ActivatorUtilities.CreateInstance<OrchestratorAIContextProvider>(_serviceProvider);

    public void Dispose()
    {
        _lock.Dispose();
    }
}

