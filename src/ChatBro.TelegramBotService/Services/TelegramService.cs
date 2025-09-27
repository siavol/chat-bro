using System.Diagnostics;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Net.Http.Json;

namespace ChatBro.TelegramBotService.Services;

public class TelegramServiceOptions
{
    public required string Token { get; init; }
}

public class TelegramService(
    IHttpClientFactory httpClientFactory,
    IOptions<TelegramServiceOptions> options,
    ILogger<TelegramService> logger) 
    : IHostedService
{
    private TelegramBotClient _telegramBot = null!;
    private static readonly ActivitySource ActivitySource = new("ChatBro.TelegramBotService");
    private readonly HttpClient _aiServiceHttpClient = httpClientFactory.CreateClient("ai-service");

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var token = options.Value.Token;
        _telegramBot = new TelegramBotClient(token);
        _telegramBot.OnMessage += OnMessage;
        return Task.CompletedTask;
    }

    private async Task OnMessage(Message message, UpdateType type)
    {
        using var activity = ActivitySource.StartActivity("TelegramService.OnMessage");
        activity?.SetTag("telegram.message.from", message.From?.ToString());
        activity?.SetTag("telegram.message.chat-id", message.Chat.Id);
        logger.LogInformation("Received message from {From} in chat {ChatId}", message.From, message.Chat.Id);
        try
        {
            if (string.IsNullOrWhiteSpace(message.Text))
            {
                logger.LogWarning("Empty message, responded with stub.");
                await _telegramBot.SendMessage(message.Chat, "Could you repeat? I received empty message.");
                return;
            }
        
            var chatRequest = new ChatRequest(message.Text);
            var response = await _aiServiceHttpClient.PostAsync("/chat", JsonContent.Create(chatRequest));
            response.EnsureSuccessStatusCode();
            var chatResponse = await response.Content.ReadFromJsonAsync<ChatResponse>()
                               ?? throw new InvalidOperationException("Could not deserialize chat response");
        
            logger.LogInformation("Sending response to telegram");
            await _telegramBot.SendMessage(message.Chat, chatResponse.TextContent);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to process telegram message");
            // TODO: move to extension method
            activity?.SetStatus(ActivityStatusCode.Error, e.Message);
            activity?.AddEvent(new ActivityEvent(
                "exception",
                tags: new ActivityTagsCollection
                {
                    { "exception.type", e.GetType().FullName },
                    { "exception.message", e.Message },
                    { "exception.stacktrace", e.ToString() }
                }));
        }        
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    // TODO: share contract?
    private record ChatRequest(string Message);
    private record ChatResponse(string TextContent);
}