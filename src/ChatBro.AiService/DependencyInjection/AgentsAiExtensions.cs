using ChatBro.AiService.Options;
using ChatBro.AiService.Plugins;
using ChatBro.AiService.Services;
using ChatBro.RestaurantsService.KernelFunction;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
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
            .AddTransient<Providers.InstructionsAIContextProvider>();

        appBuilder.Services.AddSingleton(appServices =>
        {
            var chatSettings = appServices.GetRequiredService<IOptions<ChatSettings>>();
            var openAiClient = appServices.GetRequiredService<OpenAIClient>();
            var functionMiddleware = appServices.GetRequiredService<FunctionMiddleware>();

            var aiAgent = openAiClient
                .GetChatClient(chatSettings.Value.AiModel)
                .CreateAIAgent(new ChatClientAgentOptions()
                    {
                        Name = "RestaurantsAgent",
                        Description = "An AI agent specialized in restaurant-related queries.",
                        AIContextProviderFactory = ctx => appServices.GetRequiredService<Providers.InstructionsAIContextProvider>(),
                        ChatOptions = new ChatOptions()
                        {
                            Tools = [
                                    AIFunctionFactory.Create(RestaurantsPlugin.GetRestaurants, name: "get_restaurants"),
                                    AIFunctionFactory.Create(DateTimePlugin.CurrentDateTime, name: "get_current_datetime")
                                ]
                        },
                        ChatMessageStoreFactory = ctx => new InMemoryChatMessageStore(
                            new MessageCountingChatReducer(chatSettings.Value.History.ReduceOnMessageCount),
                            ctx.SerializedState,
                            ctx.JsonSerializerOptions,
                            InMemoryChatMessageStore.ChatReducerTriggerEvent.AfterMessageAdded
                        ),
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
