using System.Collections.Generic;

namespace ChatBro.Server.Options;

public class ChatSettings
{
    public required string AiModel { get; init; }

    public ChatContextSettings Context { get; init; } = new ChatContextSettings();

    public ChatHistorySettings History { get; init; } = new ChatHistorySettings();

    public OrchestratorSettings Orchestrator { get; init; } = new OrchestratorSettings();

    public DomainCollectionSettings Domains { get; init; } = new DomainCollectionSettings();

    public class ChatContextSettings
    {
        public string Shared { get; init; } = "contexts/shared.md";
    }

    public class OrchestratorSettings
    {
        public string Name { get; init; } = "ChatBro Orchestrator";

        public string Description { get; init; } = "Routes user messages to the most relevant domain chat.";

        public string Instructions { get; init; } = "contexts/orchestrator.md";
    }

    public class DomainCollectionSettings
    {
        public DomainSettings Restaurants { get; init; } = new DomainSettings
        {
            Key = "restaurants",
            ToolName = "restaurants_chat",
            Description = "Specialist for nearby lunch ideas and restaurant intel.",
            Instructions = "contexts/domains/restaurants.md"
        };

        public DomainSettings Documents { get; init; } = new DomainSettings
        {
            Key = "documents",
            ToolName = "documents_chat",
            Description = "Specialist for Paperless document lookup and filing.",
            Instructions = "contexts/domains/documents.md"
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

        public string Instructions { get; init; } = string.Empty;
    }
}

