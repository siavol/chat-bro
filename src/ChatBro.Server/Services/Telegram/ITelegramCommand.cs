namespace ChatBro.Server.Services.Telegram;

public interface ITelegramCommand
{
    string Command { get; }
    string Description { get; }
    bool IsCommandMessage(string messageText) => messageText
        .TrimStart()
        .StartsWith($"/{Command}", StringComparison.OrdinalIgnoreCase);

    Task<string> ExecuteAsync(IServiceScope scope, string userId);
}