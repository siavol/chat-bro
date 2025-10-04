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
        appBuilder.Services.AddOptions<OpenAiSettings>()
            .BindConfiguration("OpenAI")
            .ValidateDataAnnotations()
            .ValidateOnStart();
        appBuilder.Services.AddSingleton(appServices =>
        {
            var kernelBuilder = Kernel.CreateBuilder();

            // TODO: improve logging to make it exported
            // var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            // var logger = sp.GetRequiredService(typeof(ILogger<>));
            // kernelBuilder.Services.AddSingleton(loggerFactory);
            // kernelBuilder.Services.AddSingleton(logger);
            kernelBuilder.Services.AddLogging();

            var openAiSettings = appServices.GetRequiredService<IOptions<OpenAiSettings>>();
            var openAiClient = new OpenAIClient(openAiSettings.Value.ApiKey);
            kernelBuilder.AddOpenAIChatClient(openAiSettings.Value.Model, openAiClient);
            kernelBuilder.AddOpenAIChatCompletion(openAiSettings.Value.Model, openAiClient);

            kernelBuilder.Services.ProxyScoped<RestaurantsServiceClient>(appServices);
            kernelBuilder.Plugins
                .AddFromType<RestaurantsPlugin>();

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

public class OpenAiSettings
{
    public required string Model { get; init; }
    public required string ApiKey { get; init; }
}

#pragma warning restore SKEXP0070
