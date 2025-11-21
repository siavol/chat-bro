using ChatBro.AiService.Options;
using ChatBro.AiService.Plugins;
using ChatBro.AiService.Services;
using ChatBro.RestaurantsService.KernelFunction;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;
using OpenAI;

namespace ChatBro.AiService.DependencyInjection;

#pragma warning disable MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

public static class AgentsAiExtensions
{
    public static IHostApplicationBuilder AddAgents(this IHostApplicationBuilder appBuilder)
    {
        AppContext.SetSwitch("OpenAI.Experimental.EnableOpenTelemetry", true);

        appBuilder.AddOpenAIClient("openai");

        appBuilder.Services.AddOptions<ChatSettings>()
            .BindConfiguration("Chat")
            .ValidateDataAnnotations()
            .ValidateOnStart();
        appBuilder.Services.AddSingleton<FunctionMiddleware>();
        appBuilder.Services
            .AddTransient<IContextProvider, ContextProvider>()
            .AddSingleton<IAgentThreadStore, InMemoryAgentThreadStore>()
            .AddTransient<InstructionsAIContextProvider>();

        // Register MCP client for Paperless
        appBuilder.Services.AddSingleton(async appServices =>
        {
            var configuration = appServices.GetRequiredService<IConfiguration>();
            var logger = appServices.GetRequiredService<ILogger<McpClient>>();
            var paperlessMcpUrl = configuration.GetConnectionString("paperless-mcp");
            
            if (string.IsNullOrEmpty(paperlessMcpUrl))
            {
                logger.LogWarning("Paperless MCP connection string not found. MCP tools will not be available.");
                return (McpClient?)null;
            }

            try
            {
                logger.LogInformation("Connecting to Paperless MCP server at {Url}", paperlessMcpUrl);
                var mcpClient = await McpClient.CreateAsync(new HttpClientTransport(new()
                {
                    Endpoint = new Uri(paperlessMcpUrl)
                }));
                logger.LogInformation("Successfully connected to Paperless MCP server");
                return mcpClient;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to connect to Paperless MCP server at {Url}", paperlessMcpUrl);
                return (McpClient?)null;
            }
        });

        appBuilder.Services.AddSingleton(appServices =>
        {
            var chatSettings = appServices.GetRequiredService<IOptions<ChatSettings>>();
            var openAiClient = appServices.GetRequiredService<OpenAIClient>();
            var functionMiddleware = appServices.GetRequiredService<FunctionMiddleware>();
            var logger = appServices.GetRequiredService<ILogger<AIAgent>>();

            // Get base AI tools
            var tools = new List<AITool>
            {
                AIFunctionFactory.Create(RestaurantsPlugin.GetRestaurants, name: "get_restaurants"),
                AIFunctionFactory.Create(DateTimePlugin.CurrentDateTime, name: "get_current_datetime")
            };

            // Add MCP tools if available
            try
            {
                var mcpClientTask = appServices.GetRequiredService<Task<McpClient?>>();
                var mcpClient = mcpClientTask.GetAwaiter().GetResult();
                
                if (mcpClient != null)
                {
                    var mcpTools = mcpClient.ListToolsAsync().GetAwaiter().GetResult();
                    logger.LogInformation("Retrieved {Count} tools from Paperless MCP server", mcpTools.Count);
                    tools.AddRange(mcpTools.Cast<AITool>());
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not retrieve MCP tools. Continuing without them.");
            }

            var aiAgent = openAiClient
                .GetChatClient(chatSettings.Value.AiModel)
                .CreateAIAgent(new ChatClientAgentOptions()
                    {
                        Name = "RestaurantsAgent",
                        Description = "An AI agent specialized in restaurant-related queries.",
                        AIContextProviderFactory = ctx => appServices.GetRequiredService<InstructionsAIContextProvider>(),
                        ChatOptions = new ChatOptions()
                        {
                            Tools = [.. tools]
                        },
                        ChatMessageStoreFactory = ctx =>
                        {
                            return new InMemoryChatMessageStore(
                                                        new MessageCountingChatReducer(chatSettings.Value.History.ReduceOnMessageCount),
                                                        ctx.SerializedState,
                                                        ctx.JsonSerializerOptions,
                                                        InMemoryChatMessageStore.ChatReducerTriggerEvent.AfterMessageAdded
                                                    );
                        },
                    },
                    services: appServices
                )
                .AsBuilder()
                .Use(functionMiddleware.CustomFunctionCallingMiddleware)
                .UseOpenTelemetry(
                    sourceName: "ChatBro.AiService.Agent",
                    configure: cfg => cfg.EnableSensitiveData = true)
                .Build();
            return aiAgent;
        });

        return appBuilder;
    }
}

#pragma warning restore MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
