namespace ChatBro.Server.Services.Telegram;

public class ResetCommand(ILogger<ResetCommand> logger) : ITelegramCommand
{
    public string Command => "reset";
    public string Description => "Clear AI chat history";

    public bool IsCommandMessage(string messageText) =>
        messageText.StartsWith($"/{Command}", StringComparison.OrdinalIgnoreCase);

    public async Task<string> ExecuteAsync(IServiceScope scope, string userId)
    {
        var chatService = scope.ServiceProvider.GetRequiredService<ChatService>();
        logger.LogInformation("Resetting chat history for user {UserId}", userId);
        await chatService.ResetChatAsync(userId);
        return "🧹✅";

    }
}
