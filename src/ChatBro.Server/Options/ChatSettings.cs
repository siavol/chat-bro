namespace ChatBro.Server.Options;

public class ChatSettings
{
    public required string AiModel { get; init; }

    public ChatHistorySettings History { get; init; } = new ChatHistorySettings();

    public OrchestratorSettings Orchestrator { get; init; } = new OrchestratorSettings();

    public DomainCollectionSettings Domains { get; init; } = new DomainCollectionSettings();

    public class OrchestratorSettings
    {
        public string Key => "orchestrator";

        public string Description { get; init; } = "Routes user messages to the most relevant domain chat.";
    }

    public class DomainCollectionSettings
    {
        public DomainSettings Restaurants { get; init; } = new DomainSettings
        {
            Key = "restaurants",
            ToolName = "restaurants_chat",
            Description = "Handles restaurant discovery and lunch planning."
        };

        public DomainSettings Documents { get; init; } = new DomainSettings
        {
            Key = "documents",
            ToolName = "documents_chat",
            Description = "Looks up and files Paperless documents."
        };

        public IEnumerable<DomainSettings> All()
        {
            yield return Restaurants;
            yield return Documents;
        }
    }

    public class DomainSettings
    {
        public string Key { get; init; } = string.Empty;

        public string ToolName { get; init; } = string.Empty;

        public string Description { get; init; } = string.Empty;
    }
}

