namespace ChatBro.Server.Options;

public class ChatSettings
{
    public required string AiModel { get; init; }

    public ChatContextSettings Context { get; init; } = new ChatContextSettings();

    public ChatHistorySettings History { get; init; } = new ChatHistorySettings();

    public class ChatContextSettings
    {
        public string Shared { get; init; } = "contexts/shared.md";
    }
}

