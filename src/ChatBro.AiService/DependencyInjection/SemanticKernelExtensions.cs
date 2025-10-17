using ChatBro.AiService.Options;
using ChatBro.AiService.Plugins;
using ChatBro.RestaurantsService.KernelFunction;
using Microsoft.Extensions.Options;
using OpenAI;
using Microsoft.Extensions.AI;

namespace ChatBro.AiService.DependencyInjection;

#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
public static class SemanticKernelExtensions
{
    public static IHostApplicationBuilder AddSemanticKernel(this IHostApplicationBuilder appBuilder)
    {
        AppContext.SetSwitch("Microsoft.SemanticKernel.Experimental.GenAI.EnableOTelDiagnosticsSensitive", true);
        AppContext.SetSwitch("OpenAI.Experimental.EnableOpenTelemetry", true);

        appBuilder.AddOpenAIClient("openai");
        
        appBuilder.Services.AddOptions<ChatSettings>()
            .BindConfiguration("Chat")
            .ValidateDataAnnotations()
            .ValidateOnStart();
        appBuilder.Services.AddSingleton(appServices =>
        {
            // var kernelBuilder = Kernel.CreateBuilder();
            // kernelBuilder.Services.AddLogging();

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
                );
            return aiAgent;

            // kernelBuilder.AddOpenAIChatClient(chatSettings.Value.AiModel, openAiClient);
            // kernelBuilder.AddOpenAIChatCompletion(chatSettings.Value.AiModel, openAiClient);

            // kernelBuilder.Services.ProxyScoped<RestaurantsServiceClient>(appServices);
            // kernelBuilder.Plugins
            //     .AddFromType<RestaurantsPlugin>("Restaurants")
            //     .AddFromType<DateTimePlugin>();

            // var loggingFilter = new LoggingFilter(appServices.GetRequiredService<ILogger<LoggingFilter>>());
            // kernelBuilder.Services
            //     .AddSingleton<IFunctionInvocationFilter>(loggingFilter);

            // var kernel = kernelBuilder.Build();
            // kernel.FunctionInvocationFilters.Add(loggingFilter);

            // return kernel;
        });

        return appBuilder;
    }

    private static IServiceCollection ProxyScoped<T>(this IServiceCollection services, IServiceProvider appServices)
    {
        services.AddScoped(_ => appServices.GetRequiredService<RestaurantsServiceClient>());
        return services;
    }
}

#pragma warning restore SKEXP0070
