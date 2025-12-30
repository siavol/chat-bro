using System.Text.Json;

namespace ChatBro.Server.Services.AI.Plugins;

public sealed class GenericDomainAgentAIContextProvider(
    string agentKey,
    ILoggerFactory loggerFactory) 
    : DomainAgentAIContextProvider(agentKey, loggerFactory.CreateLogger<GenericDomainAgentAIContextProvider>())
{
    public override JsonElement Serialize(JsonSerializerOptions? jsonSerializerOptions = null)
    {
        Logger.LogDebug("Serializing domain agent AI context for AgentKey: {AgentKey}", AgentKey);
        return JsonSerializer.SerializeToElement(new DummyState(), jsonSerializerOptions);
    }

    private record DummyState();
}
