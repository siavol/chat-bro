using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ChatBro.Server.Services.AI.Plugins;

public abstract class DomainAgentAIContextProvider : FileBackedAIContextProviderBase
{
    private const string InstructionsFilename = "instructions.md";
    protected ILogger Logger { get; }
    protected string AgentKey { get; }

    public override string StateKey => $"{GetType().Name}:{AgentKey}";

    public DomainAgentAIContextProvider(string agentKey, ILogger logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agentKey);
        
        this.Logger = logger;
        this.AgentKey = agentKey;
    }

    protected override async ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var instructionsPath = Path.Combine(ContextsFolder, DomainsFolder, AgentKey, InstructionsFilename);

            Logger.LogDebug("Loading domain agent system instructions for {AgentKey} from {InstructionPath}", AgentKey, instructionsPath);
            var instructions = await GetSystemContextAsync(instructionsPath, cancellationToken);
            if (string.IsNullOrWhiteSpace(instructions))
            {
                Logger.LogWarning("Domain agent system instructions for {AgentKey} at {InstructionPath} are empty.", AgentKey, instructionsPath);
                return new AIContext();
            }

            return new AIContext
            {
                Messages = [ new ChatMessage(ChatRole.System, instructions) ]
            };
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load domain agent system instructions for {AgentKey}", AgentKey);
            throw;
        }
    }
}
