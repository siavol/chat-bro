using System;
using System.Diagnostics;
using ChatBro.ServiceDefaults;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ChatBro.Server.Services.Telegram;

public sealed class TelegramBotHostedService(
    IOptions<TelegramServiceOptions> options,
    IServiceScopeFactory scopeFactory,
    MessageSplitter splitter,
    ILogger<TelegramBotHostedService> logger,
    ActivitySource activitySource)
    : IHostedService, IDisposable
{
    private readonly TelegramServiceOptions _telegramOptions = options.Value;
    private TelegramBotClient? _telegramBotClient;
    private CancellationTokenSource? _receiverCts;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _telegramBotClient = new TelegramBotClient(_telegramOptions.Token);
        _receiverCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = [UpdateType.Message]
        };

        _telegramBotClient.StartReceiving(
            HandleUpdateAsync,
            HandlePollingErrorAsync,
            receiverOptions,
            _receiverCts.Token);

        await RegisterCommandsAsync(cancellationToken);

        logger.LogInformation("Telegram bot polling started.");
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

        var userId = message.Chat.Id.ToString();
        var trimmedMessage = message.Text.Trim();
        if (IsResetCommand(trimmedMessage))
        {
            await HandleResetCommandAsync(botClient, message.Chat, userId, cancellationToken);
            return;
        }

        try
        {
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

    private static bool IsResetCommand(string messageText) =>
        messageText.StartsWith("/reset", StringComparison.OrdinalIgnoreCase);

    private async Task HandleResetCommandAsync(ITelegramBotClient botClient, Chat chat, string userId, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var chatService = scope.ServiceProvider.GetRequiredService<ChatService>();
            var historyCleared = await chatService.ResetChatAsync(userId);
            var reply = historyCleared
                ? "Your AI chat history has been cleared."
                : "There was no stored AI chat history, you are already starting fresh.";

            await botClient.SendMessage(chat, reply, cancellationToken: cancellationToken);
            logger.LogInformation("Reset command handled for user {UserId}, cleared={Cleared}", userId, historyCleared);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to handle reset command for user {UserId}", userId);
            await SendErrorMessageAsync(botClient, chat, ex, cancellationToken);
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

    private async Task RegisterCommandsAsync(CancellationToken cancellationToken)
    {
        if (_telegramBotClient is null)
        {
            return;
        }

        var commands = new[]
        {
            new BotCommand
            {
                Command = "reset",
                Description = "Clear AI chat history"
            }
        };

        try
        {
            await _telegramBotClient.SetMyCommands(commands, cancellationToken: cancellationToken);
            logger.LogInformation("Registered Telegram bot commands");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to register Telegram bot commands");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (_receiverCts is not null && !_receiverCts.IsCancellationRequested)
        {
            _receiverCts.Cancel();
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _receiverCts?.Dispose();
    }
}

