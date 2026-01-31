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

            logger.LogInformation("Building orchestrator and domain agents");
            _domainAgents = await BuildDomainAgentsAsync();
            _orchestratorAgent = BuildOrchestratorAgent();
            logger.LogInformation("Constructed orchestrator agent plus {Count} domain agents", _domainAgents.Count);
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

        // little bit strange way to get IChatClient...
        var chatClient = openAiClient.GetChatClient(_chatSettings.AiModel).AsIChatClient();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        var options = new ChatClientAgentOptions
        {
            Name = "RestaurantsAgent",
            Description = domainSettings.Description,
            AIContextProviderFactory = (ctx, _) => ValueTask.FromResult<AIContextProvider>(
                new RestaurantsAgentAIContextProvider(domainSettings.Key, 
                    chatClient, loggerFactory, ctx.SerializedState, ctx.JsonSerializerOptions)),
            ChatOptions = new ChatOptions
            {
                Tools = tools.ToArray()
            }
        };
        var agent = CreateAgent(options);
        
        var registration = DomainAgentRegistration.Create(domainSettings, agent);

        return Task.FromResult(registration);
    }

    private async Task<DomainAgentRegistration> BuildDocumentsDomainAgentAsync()
    {
        var domainSettings = _chatSettings.Domains.Documents;
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var tools = await paperlessMcpClient.GetToolsAsync();

        var agentOptions = new ChatClientAgentOptions
        {
            Name = "DocumentsAgent",
            Description = domainSettings.Description,
            AIContextProviderFactory = (_, _) => ValueTask.FromResult<AIContextProvider>(
                new GenericDomainAgentAIContextProvider(domainSettings.Key, loggerFactory)),
            ChatOptions = new ChatOptions
            {
                Tools = tools.ToArray()
            }
        };
        var agent = CreateAgent(agentOptions);

        return DomainAgentRegistration.Create(domainSettings, agent);
    }

    private AIAgent BuildOrchestratorAgent()
    {
        var orchestrator = _chatSettings.Orchestrator;
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var tools = new List<AITool>
        {
            AIFunctionFactory.Create(DateTimePlugin.CurrentDateTime, name: "get_current_datetime")
        };

        var agentOptions = new ChatClientAgentOptions
        {
            Name = "OrchestratorAgent",
            Description = orchestrator.Description,
            AIContextProviderFactory = (_, _) => ValueTask.FromResult<AIContextProvider>(
                new OrchestratorAIContextProvider(_chatSettings.Domains.All(), loggerFactory)),
            ChatOptions = new ChatOptions
            {
                Tools = tools.ToArray()
            }
        };


        return CreateAgent(agentOptions);
    }

    private AIAgent CreateAgent(ChatClientAgentOptions agentOptions)
    {
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        var chatClient = openAiClient.GetChatClient(_chatSettings.AiModel).AsIChatClient();
        return new ChatClientAgent(
                chatClient,
                agentOptions,
                loggerFactory,
                serviceProvider)
            .AsBuilder()
            .Use(functionMiddleware.CustomFunctionCallingMiddleware)
            .UseOpenTelemetry(
                configure: cfg => cfg.EnableSensitiveData = true)
            .Build();
    }

    public void Dispose()
    {
        _lock.Dispose();
    }
}

