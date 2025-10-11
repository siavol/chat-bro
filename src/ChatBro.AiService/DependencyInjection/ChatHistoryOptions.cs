namespace ChatBro.AiService.DependencyInjection;

public class ChatHistoryOptions
{
    /// Sliding expiration in minutes for per-user chat session entries
    public int SlidingExpirationMinutes { get; init; } = 30;

    /// Absolute expiration in hours for per-user chat session entries
    public int AbsoluteExpirationHours { get; init; } = 24;

    /// Maximum number of messages (user+assistant turns) to keep in history
    public int MaxMessages { get; init; } = 40;

    /// Soft limit for tokens (estimate) to keep in history; implementations may approximate
    public int MaxTokens { get; init; } = 4000;
}
