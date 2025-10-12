namespace ChatBro.AiService.Options;

public class ChatHistorySettings
{
    /// Sliding expiration in minutes for per-user chat session entries
    public int SlidingExpirationMinutes { get; init; } = 30;

    /// Absolute expiration in hours for per-user chat session entries
    public int AbsoluteExpirationHours { get; init; } = 24;
}
