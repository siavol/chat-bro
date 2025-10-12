namespace ChatBro.AiService.Options;

public class ChatSettings
{
    public required string AiModel { get; init; }

    public ChatContextSettings Context { get; init; } = new ChatContextSettings();

    public class ChatContextSettings
    {
        public string Shared { get; init; } = "contexts/shared.md";
    }
}
