using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ChatBro.TelegramBotService.Services;

public class TelegramServiceOptions
{
    public required string Token { get; init; }
}

public class TelegramService(IOptions<TelegramServiceOptions> options) : IHostedService
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
        var text = $"{message.From} said: {message.Text}. Confirming message received. Channel id: {message.Chat.Id}";
        await _telegramBot.SendMessage(message.Chat, text);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}