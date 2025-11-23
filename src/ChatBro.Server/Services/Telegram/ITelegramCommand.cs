namespace ChatBro.Server.Services.Telegram;

public interface ITelegramCommand
{
    string Command { get; }
    string Description { get; }
    bool IsCommandMessage(string messageText);
    Task<string> ExecuteAsync(IServiceScope scope, string userId);
}