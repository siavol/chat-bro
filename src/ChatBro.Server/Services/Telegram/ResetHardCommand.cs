using ChatBro.Server.Services.AI;

namespace ChatBro.Server.Services.Telegram;

public class ResetHardCommand(ILogger<ResetHardCommand> logger) : ITelegramCommand
{
    public string Command => "reset_hard";
    public string Description => "Clear AI chat history and observational memory";

    public async Task<string> ExecuteAsync(IServiceScope scope, string userId)
    {
        var chatService = scope.ServiceProvider.GetRequiredService<ChatService>();
        logger.LogInformation("Hard resetting chat history and memory for user {UserId}", userId);
        await chatService.HardResetChatAsync(userId);
        return "🧹🧠✅";
    }
}
