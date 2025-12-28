using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ChatBro.Server.Services.AI.Plugins;

public abstract class FileBackedAIContextProvider(IContextProvider contextProvider, ILogger logger) : AIContextProvider
{
    protected abstract string StateKey { get; }

    protected abstract string GetInstructionsPath();

    public override async ValueTask<AIContext> InvokingAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var instructionsPath = GetInstructionsPath();

            logger.LogDebug("Loading system instructions for {StateKey} from {InstructionPath}", StateKey, instructionsPath);
            var instructions = await contextProvider.GetSystemContextAsync(instructionsPath);
            if (string.IsNullOrWhiteSpace(instructions))
            {
                logger.LogWarning("Agent system instructions for {StateKey} at {InstructionPath} are empty.", StateKey, instructionsPath);
                return new AIContext();
            }

            return new AIContext
            {
                Messages = [ new ChatMessage(ChatRole.System, instructions) ]
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load system instructions for {StateKey}", StateKey);
            throw;
        }
    }

    public override JsonElement Serialize(JsonSerializerOptions? jsonSerializerOptions = null)
    {
        logger.LogDebug("Serializing AI context StateKey: {StateKey}", StateKey);
        return JsonSerializer.SerializeToElement(new InternalState(StateKey), jsonSerializerOptions);
    }

    internal record InternalState(string AgentKey);
}

public sealed class DomainAgentAIContextProvider(
    IContextProvider contextProvider,
    ILogger<DomainAgentAIContextProvider> logger,
    string agentKey) : FileBackedAIContextProvider(contextProvider, logger)
{
    protected override string StateKey => agentKey;

    protected override string GetInstructionsPath()
    {
        return $"contexts/domains/{agentKey}/instructions.md";
    }
}

public sealed class OrchestratorAIContextProvider(
    IContextProvider contextProvider,
    ILogger<OrchestratorAIContextProvider> logger) : FileBackedAIContextProvider(contextProvider, logger)
{
    protected override string StateKey => "orchestrator";

    protected override string GetInstructionsPath()
    {
        return "contexts/orchestrator.md";
    }
}
