using System.Diagnostics;
using ChatBro.Server.Services;
using ChatBro.ServiceDefaults;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ChatBro.Server.Services.Telegram;

public class TelegramBotHostedService(
    IOptions<TelegramServiceOptions> options,
    IServiceScopeFactory scopeFactory,
    MessageSplitter splitter,
    ILogger<TelegramBotHostedService> logger,
    ActivitySource activitySource)
    : IHostedService
{
    private readonly TelegramServiceOptions _telegramOptions = options.Value;
    private TelegramBotClient? _telegramBotClient;
    private CancellationTokenSource? _receiverCts;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _telegramBotClient = new TelegramBotClient(_telegramOptions.Token);
        _receiverCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = new[] { UpdateType.Message }
        };

        _telegramBotClient.StartReceiving(
            HandleUpdateAsync,
            HandlePollingErrorAsync,
            receiverOptions,
            _receiverCts.Token);

        logger.LogInformation("Telegram bot polling started.");
        return Task.CompletedTask;
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { } message)
        {
            return;
        }

        using var activity = activitySource.StartActivity(ActivityKind.Consumer);
        activity?.SetTag("telegram.message.from", message.From?.ToString());
        activity?.SetTag("telegram.message.chat-id", message.Chat.Id);
        logger.LogInformation("Received message from {From} in chat {ChatId}", message.From, message.Chat.Id);

        if (string.IsNullOrWhiteSpace(message.Text))
        {
            logger.LogWarning("Empty message, responded with stub.");
            await botClient.SendMessage(
                message.Chat,
                "Could you repeat? I received empty message.",
                cancellationToken: cancellationToken);
            return;
        }

        try
        {
            var userId = message.Chat.Id.ToString();
            string replyText;
            using (var scope = scopeFactory.CreateScope())
            {
                var chatService = scope.ServiceProvider.GetRequiredService<ChatService>();
                replyText = await chatService.GetChatResponseAsync(message.Text, userId);
            }

            logger.LogInformation("AI generated response, length {Length}", replyText.Length);
            foreach (var replyMessage in splitter.SplitSmart(replyText))
            {
                logger.LogInformation("Sending response to telegram");
                await botClient.SendMessage(
                    message.Chat,
                    replyMessage,
                    cancellationToken: cancellationToken);
            }

            logger.LogInformation("Sent full response to telegram");
        }
        catch (ApiRequestException e)
        {
            logger.LogError(e, "Telegram API returned error {ErrorMessage} with error code {ErrorCode}", e.Message, e.ErrorCode);
            activity?.SetException(e);
            await SendErrorMessageAsync(botClient, message.Chat, e, cancellationToken);
            throw;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to process telegram message");
            activity?.SetException(e);
            await SendErrorMessageAsync(botClient, message.Chat, e, cancellationToken);
            throw;
        }
    }

    private async Task SendErrorMessageAsync(ITelegramBotClient botClient, Chat chat, Exception exception, CancellationToken cancellationToken)
    {
        await botClient.SendMessage(
            chat,
            $"Sorry, I encountered an error while processing your message. Error: {exception.Message}",
            cancellationToken: cancellationToken);
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Telegram polling failed");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_receiverCts is not null && !_receiverCts.IsCancellationRequested)
        {
            _receiverCts.Cancel();
        }

        return Task.CompletedTask;
    }
}

