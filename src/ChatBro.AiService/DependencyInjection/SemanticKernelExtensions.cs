using ChatBro.AiService.Plugins;
using Microsoft.SemanticKernel;
using OllamaSharp;

namespace ChatBro.AiService.DependencyInjection;

#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
public static class SemanticKernelExtensions
{
    public static IHostApplicationBuilder AddSemanticKernel(
        this IHostApplicationBuilder appBuilder,
        string connectinName)
    {
        appBuilder.AddOllamaApiClient(connectinName);

        appBuilder.Services.AddSingleton(appServices =>
        {
            var ollamaApiClient = appServices.GetRequiredService<IOllamaApiClient>();
            var concreteOllamaApiClient = (OllamaApiClient)ollamaApiClient;

            var kernelBuilder = Kernel.CreateBuilder();

            // Proxy services to the kernel builder
            // kernelBuilder.Services.AddScoped(_ => appServices.GetRequiredService<RadarrClient>());

            // TODO: improve logging to make it exported
            // var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            // var logger = sp.GetRequiredService(typeof(ILogger<>));
            // kernelBuilder.Services.AddSingleton(loggerFactory);
            // kernelBuilder.Services.AddSingleton(logger);
            kernelBuilder.Services.AddLogging();

            kernelBuilder.AddOllamaChatClient(ollamaClient: concreteOllamaApiClient);
            kernelBuilder.AddOllamaChatCompletion(ollamaClient: concreteOllamaApiClient);

            // kernelBuilder.Plugins
            //     .AddFromType<RadarrPlugin>();

            var loggingFilter = new LoggingFilter(appServices.GetRequiredService<ILogger<LoggingFilter>>());
            kernelBuilder.Services
                .AddSingleton<IFunctionInvocationFilter>(loggingFilter);

            var kernel = kernelBuilder.Build();
            kernel.FunctionInvocationFilters.Add(loggingFilter);

            return kernel;
        });

        return appBuilder;
    }
}
