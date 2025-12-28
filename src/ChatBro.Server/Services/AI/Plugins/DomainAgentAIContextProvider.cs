using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ChatBro.Server.Services.AI.Plugins;

public sealed class DomainAgentAIContextProvider : FileBackedAIContextProviderBase
{
    private const string InstructionsFilename = "instructions.md";
    private readonly ILogger<DomainAgentAIContextProvider> _logger;
    private readonly string _agentKey;

    public DomainAgentAIContextProvider(
        ILogger<DomainAgentAIContextProvider> logger,
        string agentKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentKey);
        
        _logger = logger;
        _agentKey = agentKey;
    }

    public override async ValueTask<AIContext> InvokingAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var instructionsPath = Path.Combine(ContextsFolder, DomainsFolder, _agentKey, InstructionsFilename);

            _logger.LogDebug("Loading domain agent system instructions for {AgentKey} from {InstructionPath}", _agentKey, instructionsPath);
            var instructions = await GetSystemContextAsync(instructionsPath, cancellationToken);
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
