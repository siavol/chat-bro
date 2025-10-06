using ChatBro.AiService.Plugins;
using ChatBro.RestaurantsService.KernelFunction;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using OpenAI;

namespace ChatBro.AiService.DependencyInjection;

#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
public static class SemanticKernelExtensions
{
    public static IHostApplicationBuilder AddSemanticKernel(this IHostApplicationBuilder appBuilder)
    {
        AppContext.SetSwitch("Microsoft.SemanticKernel.Experimental.GenAI.EnableOTelDiagnosticsSensitive", true);
        AppContext.SetSwitch("OpenAI.Experimental.EnableOpenTelemetry", true);
        
        appBuilder.AddOpenAIClient("openai");
        
        appBuilder.Services.AddOptions<OpenAiSettings>()
            .BindConfiguration("OpenAI")
            .ValidateDataAnnotations()
            .ValidateOnStart();
        appBuilder.Services.AddSingleton(appServices =>
        {
            var kernelBuilder = Kernel.CreateBuilder();
            kernelBuilder.Services.AddLogging();

            var openAiSettings = appServices.GetRequiredService<IOptions<OpenAiSettings>>();
            var openAiClient = appServices.GetRequiredService<OpenAIClient>();
            kernelBuilder.AddOpenAIChatClient(openAiSettings.Value.Model, openAiClient);
            kernelBuilder.AddOpenAIChatCompletion(openAiSettings.Value.Model, openAiClient);

            kernelBuilder.Services.ProxyScoped<RestaurantsServiceClient>(appServices);
            kernelBuilder.Plugins
                .AddFromType<RestaurantsPlugin>("Restaurants")
                .AddFromType<DateTimePlugin>();

            var loggingFilter = new LoggingFilter(appServices.GetRequiredService<ILogger<LoggingFilter>>());
            kernelBuilder.Services
                .AddSingleton<IFunctionInvocationFilter>(loggingFilter);

            var kernel = kernelBuilder.Build();
            kernel.FunctionInvocationFilters.Add(loggingFilter);

            return kernel;
        });

        return appBuilder;
    }

    private static IServiceCollection ProxyScoped<T>(this IServiceCollection services, IServiceProvider appServices)
    {
        services.AddScoped(_ => appServices.GetRequiredService<RestaurantsServiceClient>());
        return services;
    }
}

// TODO: it now confuses with the Aspire OpenAI settings, needs to be reorganised 
public class OpenAiSettings
{
    public required string Model { get; init; }
}

#pragma warning restore SKEXP0070
