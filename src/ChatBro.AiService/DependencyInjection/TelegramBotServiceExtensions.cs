using System.Diagnostics;
using ChatBro.AiService.Services.Telegram;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;

namespace ChatBro.AiService.DependencyInjection;

public static class TelegramBotServiceExtensions
{
    private const string ActivitySourceName = "ChatBro.TelegramBotService";

    public static IHostApplicationBuilder AddTelegramBotIfConfigured(this IHostApplicationBuilder builder)
    {
        var telegramSection = builder.Configuration.GetSection("Telegram");
        var token = telegramSection.GetValue<string>(nameof(TelegramServiceOptions.Token));

        if (string.IsNullOrWhiteSpace(token))
        {
            builder.Services.AddHostedService<TelegramBotDisabledHostedService>();
            return builder;
        }

        builder.Services.AddOptions<TelegramServiceOptions>()
            .Bind(telegramSection)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddSingleton(new ActivitySource(ActivitySourceName));
        builder.Services.AddSingleton<MessageSplitter>();
        builder.Services.AddHostedService<TelegramBotHostedService>();

        builder.Services.AddOpenTelemetry().WithTracing(tracing =>
        {
            tracing.AddProcessor(new TelegramApiFilterProcessor());
        });

        return builder;
    }

    private sealed class TelegramBotDisabledHostedService(ILogger<TelegramBotDisabledHostedService> logger)
        : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Telegram bot not configured. Set Telegram__Token to enable it.");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
