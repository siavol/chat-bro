using System.Diagnostics;
using ChatBro.Server.Services.Telegram;
using OpenTelemetry.Trace;

namespace ChatBro.Server.DependencyInjection;

public static class TelegramBotServiceExtensions
{
    private const string ActivitySourceName = "ChatBro.TelegramBotService";

    public static IHostApplicationBuilder AddTelegramBot(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOptions<TelegramServiceOptions>()
            .BindConfiguration("Telegram")
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddSingleton(new ActivitySource(ActivitySourceName));
        builder.Services
            .AddSingleton<MessageSplitter>()
            .AddTransient<ITelegramCommand, ResetCommand>();
        builder.Services.AddHostedService<TelegramBotHostedService>();

        builder.Services.AddOpenTelemetry().WithTracing(tracing =>
        {
            tracing.AddProcessor(new TelegramApiFilterProcessor());
        });

        return builder;
    }
}

