using ChatBro.AiService.Options;
using ChatBro.AiService.Plugins;
using ChatBro.RestaurantsService.KernelFunction;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;

namespace ChatBro.AiService.Services;

#pragma warning disable MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

public class AIAgentProvider(
    IOptions<ChatSettings> chatSettings,
    OpenAIClient openAiClient,
    FunctionMiddleware functionMiddleware,
    PaperlessMcpClient paperlessMcpClient,
    IServiceProvider serviceProvider,
    ILogger<AIAgentProvider> logger) : IAIAgentProvider
{
    private readonly IOptions<ChatSettings> _chatSettings = chatSettings;
    private readonly OpenAIClient _openAiClient = openAiClient;
    private readonly FunctionMiddleware _functionMiddleware = functionMiddleware;
    private readonly PaperlessMcpClient _paperlessMcpClient = paperlessMcpClient;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<AIAgentProvider> _logger = logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private AIAgent? _cachedAgent;

    public async Task<AIAgent> GetAgentAsync()
    {
        if (_cachedAgent != null)
        {
            return _cachedAgent;
        }

        await _lock.WaitAsync();
        try
        {
            if (_cachedAgent != null)
            {
                return _cachedAgent;
            }

            _logger.LogInformation("Creating AI agent with tools");

            // Get base AI tools
            var tools = new List<AITool>
            {
                AIFunctionFactory.Create(RestaurantsPlugin.GetRestaurants, name: "get_restaurants"),
                AIFunctionFactory.Create(DateTimePlugin.CurrentDateTime, name: "get_current_datetime")
            };

            // Add Paperless MCP tools asynchronously
            var mcpTools = await _paperlessMcpClient.GetToolsAsync();
            tools.AddRange(mcpTools);
            _logger.LogInformation("Added {Count} Paperless MCP tools to agent", mcpTools.Count);

            var aiAgent = _openAiClient
                .GetChatClient(_chatSettings.Value.AiModel)
                .CreateAIAgent(new ChatClientAgentOptions()
                    {
                        Name = "RestaurantsAgent",
                        Description = "An AI agent specialized in restaurant-related queries.",
                        AIContextProviderFactory = ctx => _serviceProvider.GetRequiredService<InstructionsAIContextProvider>(),
                        ChatOptions = new ChatOptions()
                        {
                            Tools = [.. tools]
                        },
                        ChatMessageStoreFactory = ctx =>
                        {
                            return new InMemoryChatMessageStore(
                                new MessageCountingChatReducer(_chatSettings.Value.History.ReduceOnMessageCount),
                                ctx.SerializedState,
                                ctx.JsonSerializerOptions,
                                InMemoryChatMessageStore.ChatReducerTriggerEvent.AfterMessageAdded
                            );
                        },
                    },
                    services: _serviceProvider
                )
                .AsBuilder()
                .Use(_functionMiddleware.CustomFunctionCallingMiddleware)
                .UseOpenTelemetry(
                    sourceName: "ChatBro.AiService.Agent",
                    configure: cfg => cfg.EnableSensitiveData = true)
                .Build();

            _cachedAgent = aiAgent;
            _logger.LogInformation("AI agent created and cached");
            return aiAgent;
        }
        finally
        {
            _lock.Release();
        }
    }
}
