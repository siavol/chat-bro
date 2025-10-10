using System.Diagnostics;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ChatBro.TelegramBotService.Clients;
using Telegram.Bot.Exceptions;
using ChatBro.ServiceDefaults;

namespace ChatBro.TelegramBotService.Services;

public class TelegramServiceOptions
{
    public required string Token { get; init; }
}

public class TelegramService(
    IOptions<TelegramServiceOptions> options,
    AiServiceClient aiServiceClient,
    MessageSplitter splitter,
    ILogger<TelegramService> logger,
    ActivitySource activitySource)
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
        using var activity = activitySource.StartActivity(ActivityKind.Consumer);
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

            var replyText = await aiServiceClient.ChatAsync(message.Text);
            logger.LogInformation("AI generated response, length {Length}", replyText.Length);

            foreach (var replyMessage in splitter.SplitSmart(replyText))
            {
                logger.LogInformation("Sending response to telegram");
                await _telegramBot.SendMessage(message.Chat, replyMessage);
            }
            logger.LogInformation("Sent full response to telegram");
        }
        catch (ApiRequestException e)
        {
            logger.LogError(e, "Telegram API returned error {ErrorMessage} with error code {ErrorCode}",
                e.Message, e.ErrorCode);
            activity.SetException(e);
            await SendErrorMessageToChat(message.Chat, e);
            throw;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to process telegram message");
            activity.SetException(e);
            await SendErrorMessageToChat(message.Chat, e);
            throw;
        }
    }

    private async Task SendErrorMessageToChat(Chat chat, Exception e)
    {
        await _telegramBot.SendMessage(chat, $"Sorry, I encountered an error while processing your message. Error: {e.Message}");
    }

    Task IHostedService.StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}