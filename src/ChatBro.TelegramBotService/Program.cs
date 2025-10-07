using System.Diagnostics;
using OpenTelemetry.Trace;

using ChatBro.TelegramBotService.Clients;
using ChatBro.TelegramBotService.Services;
using ChatBro.TelegramBotService.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton(new ActivitySource("ChatBro.TelegramBotService"));

// Add OpenTelemetry tracing and register the custom filter processor
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddProcessor(new TelegramApiFilterProcessor());
    });

builder.Services.AddHttpClient<AiServiceClient>(
    static client => client.BaseAddress = new("https+http://chatbro-ai-service"));

builder.Services.AddOptions<TelegramServiceOptions>()
    .BindConfiguration("Telegram")
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services
    .AddSingleton<MessageSplitter>()
    .AddHostedService<TelegramService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapDefaultEndpoints();

app.Run();
