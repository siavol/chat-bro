using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ChatBro.Server.Services.AI.Plugins;

public sealed class DomainAgentAIContextProvider : AIContextProvider
{
    private readonly IContextProvider _contextProvider;
    private readonly ILogger<DomainAgentAIContextProvider> _logger;
    private readonly string _agentKey;

    public DomainAgentAIContextProvider(
        IContextProvider contextProvider,
        ILogger<DomainAgentAIContextProvider> logger,
        string agentKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentKey);
        
        _contextProvider = contextProvider;
        _logger = logger;
        _agentKey = agentKey;
    }

    public override async ValueTask<AIContext> InvokingAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var instructionsPath = $"contexts/domains/{_agentKey}/instructions.md";

            _logger.LogDebug("Loading domain agent system instructions for {AgentKey} from {InstructionPath}", _agentKey, instructionsPath);
            var instructions = await _contextProvider.GetSystemContextAsync(instructionsPath);
            if (string.IsNullOrWhiteSpace(instructions))
            {
                _logger.LogWarning("Domain agent system instructions for {AgentKey} at {InstructionPath} are empty.", _agentKey, instructionsPath);
                return new AIContext();
            }

            return new AIContext
            {
                Messages = [ new ChatMessage(ChatRole.System, instructions) ]
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load domain agent system instructions for {AgentKey}", _agentKey);
            throw;
        }
    }

    public override JsonElement Serialize(JsonSerializerOptions? jsonSerializerOptions = null)
    {
        _logger.LogDebug("Serializing domain agent AI context for AgentKey: {AgentKey}", _agentKey);
        return JsonSerializer.SerializeToElement(new InternalState(_agentKey), jsonSerializerOptions);
    }

    internal record InternalState(string AgentKey);
}
