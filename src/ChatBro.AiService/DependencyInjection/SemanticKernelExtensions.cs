using ChatBro.AiService.Options;
using ChatBro.AiService.Plugins;
using ChatBro.RestaurantsService.KernelFunction;
using Microsoft.Extensions.Options;
using OpenAI;
using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;

namespace ChatBro.AiService.DependencyInjection;

public static class SemanticKernelExtensions
{
    public static IHostApplicationBuilder AddSemanticKernel(this IHostApplicationBuilder appBuilder)
    {
        AppContext.SetSwitch("OpenAI.Experimental.EnableOpenTelemetry", true);

        appBuilder.AddOpenAIClient("openai");
        
        appBuilder.Services.AddOptions<ChatSettings>()
            .BindConfiguration("Chat")
            .ValidateDataAnnotations()
            .ValidateOnStart();
        appBuilder.Services.AddSingleton(appServices =>
        {
            var chatSettings = appServices.GetRequiredService<IOptions<ChatSettings>>();
            var openAiClient = appServices.GetRequiredService<OpenAIClient>();
            var aiAgent = openAiClient
                .GetChatClient(chatSettings.Value.AiModel)
                .CreateAIAgent(
                    name: "RestaurantsAgent",
                    description: "An AI agent specialized in restaurant-related queries.",
                    tools: [
                        AIFunctionFactory.Create(RestaurantsPlugin.GetRestaurants, name: "get_restaurants"),
                        AIFunctionFactory.Create(DateTimePlugin.CurrentDateTime, name: "get_current_datetime")
                    ],
                    services: appServices
                )
                .AsBuilder()
                .UseOpenTelemetry(
                    sourceName: "ChatBro.AiService.Agent",
                    configure: cfg => cfg.EnableSensitiveData = true)
                .Build();
            return aiAgent;
        });

        return appBuilder;
    }
}
