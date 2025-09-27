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
    HttpClient aiServiceHttpClient,
    IOptions<TelegramServiceOptions> options) 
    : IHostedService
{
    private TelegramBotClient _telegramBot = null!;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var token = options.Value.Token;
        _telegramBot = new TelegramBotClient(token);
        _telegramBot.OnMessage += OnMessage;
        return Task.CompletedTask;
    }

    private async Task OnMessage(Message message, UpdateType type)
    {
        Activity.Current?.AddTag("telegram.message.from", message.From?.ToString());
        Activity.Current?.AddTag("telegram.message.chat-id", message.Chat.Id);
        
        if (string.IsNullOrWhiteSpace(message.Text))
        {
            await _telegramBot.SendMessage(message.Chat, "Could you repeat? I received empty message.");
            return;
        }
        
        var chatRequest = new ChatRequest(message.Text);
        var response = await aiServiceHttpClient.PostAsync("/chat", JsonContent.Create(chatRequest));
        response.EnsureSuccessStatusCode();
        var chatResponse = await response.Content.ReadFromJsonAsync<ChatResponse>()
            ?? throw new InvalidOperationException("Could not deserialize chat response");
        
        await _telegramBot.SendMessage(message.Chat, chatResponse.TextContent);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    // TODO: share contract?
    private record ChatRequest(string Message);
    private record ChatResponse(string TextContent);
}