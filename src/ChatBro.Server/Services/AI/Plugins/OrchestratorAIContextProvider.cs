using System.Text;
using System.Text.Json;
using ChatBro.Server.Options;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ChatBro.Server.Services.AI.Plugins;

public sealed class OrchestratorAIContextProvider(
    IContextProvider contextProvider,
    ILogger<OrchestratorAIContextProvider> logger,
    IEnumerable<ChatSettings.DomainSettings> domainSettings) : AIContextProvider
{
    private const string InstructionsPath = "contexts/orchestrator.md";
    private const string AgentDescriptionsPlaceholder = "<agent-descriptions-here>";

    public override async ValueTask<AIContext> InvokingAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Loading orchestrator system instructions from {InstructionPath}", InstructionsPath);
            var baseInstructions = await contextProvider.GetSystemContextAsync(InstructionsPath);
            if (string.IsNullOrWhiteSpace(baseInstructions))
            {
                logger.LogWarning("Orchestrator system instructions at {InstructionPath} are empty.", InstructionsPath);
                return new AIContext();
            }

            var domainDescriptions = await LoadDomainDescriptionsAsync(cancellationToken);
            var combinedInstructions = baseInstructions.Replace(AgentDescriptionsPlaceholder, domainDescriptions);

            return new AIContext
            {
                Messages = [ new ChatMessage(ChatRole.System, combinedInstructions) ]
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load orchestrator system instructions from {InstructionPath}", InstructionsPath);
            throw;
        }
    }

    private async Task<string> LoadDomainDescriptionsAsync(CancellationToken cancellationToken)
    {
        var descriptionsBuilder = new StringBuilder();

        foreach (var domain in domainSettings)
        {
            try
            {
                var descriptionPath = $"contexts/domains/{domain.Key}/description.md";
                logger.LogDebug("Loading domain description for {DomainKey} from {DescriptionPath}", domain.Key, descriptionPath);
                
                var description = await contextProvider.GetSystemContextAsync(descriptionPath);
                
                if (!string.IsNullOrWhiteSpace(description))
                {
                    descriptionsBuilder.AppendLine($"## {domain.ToolName}");
                    descriptionsBuilder.AppendLine(description.Trim());
                    descriptionsBuilder.AppendLine();
                }
                else
                {
                    logger.LogWarning("Domain description for {DomainKey} at {DescriptionPath} is empty.", domain.Key, descriptionPath);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load domain description for {DomainKey}", domain.Key);
            }
        }

        return descriptionsBuilder.ToString();
    }

    public override JsonElement Serialize(JsonSerializerOptions? jsonSerializerOptions = null)
    {
        logger.LogDebug("Serializing orchestrator AI context");
        return JsonSerializer.SerializeToElement(new InternalState("Orchestrator"), jsonSerializerOptions);
    }

    internal record InternalState(string StateKey);
}
