using ChatBro.Server.Options;
using ChatBro.Server.Services;
using ChatBro.Server.Services.AI;
using ChatBro.Server.Services.AI.Plugins;

namespace ChatBro.Server.DependencyInjection;

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
            .AddSingleton<IAgentSessionStore, InMemoryAgentSessionStore>()
            .AddSingleton<IDomainToolingBuilder, DomainToolingBuilder>();

        // Register MCP client for Paperless
        appBuilder.Services.AddSingleton<PaperlessMcpClient>();

        // Register AI Agent Provider
        appBuilder.Services.AddSingleton<IAIAgentProvider, AIAgentProvider>();

        return appBuilder;
    }
}

