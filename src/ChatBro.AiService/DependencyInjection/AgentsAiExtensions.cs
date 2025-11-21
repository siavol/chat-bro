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
        appBuilder.Services.AddOptions<PaperlessMcpOptions>()
            .Configure<IConfiguration>((options, configuration) =>
            {
                options.ConnectionString = configuration.GetConnectionString("paperless-mcp") ?? string.Empty;
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();
        appBuilder.Services.AddSingleton<FunctionMiddleware>();
        appBuilder.Services
            .AddTransient<IContextProvider, ContextProvider>()
            .AddSingleton<IAgentThreadStore, InMemoryAgentThreadStore>()
            .AddTransient<InstructionsAIContextProvider>();

        // Register MCP client for Paperless
        appBuilder.Services.AddSingleton<PaperlessMcpClient>();

        // Register AI Agent Provider
        appBuilder.Services.AddSingleton<IAIAgentProvider, AIAgentProvider>();

        return appBuilder;
    }
}

#pragma warning restore MEAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
