using System.Text;
using System.Text.Json;
using ChatBro.Server.Options;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ChatBro.Server.Services.AI.Plugins;

public sealed class OrchestratorAIContextProvider : FileBackedAIContextProviderBase
{
    private const string OrchestratorInstructionsFilename = "orchestrator.md";
    private const string DomainDescriptionFilename = "description.md";
    private const string AgentDescriptionsPlaceholder = "<agent-descriptions-here>";
    
    private readonly ILogger<OrchestratorAIContextProvider> logger;
    private readonly List<ChatSettings.DomainSettings> domainSettings;

    public OrchestratorAIContextProvider(
        IEnumerable<ChatSettings.DomainSettings> domainSettings,
        ILoggerFactory loggerFactory)
    {
        logger = loggerFactory.CreateLogger<OrchestratorAIContextProvider>();
        this.domainSettings = [.. domainSettings];
    }

    public override async ValueTask<AIContext> InvokingAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        var instructionsPath = Path.Combine(ContextsFolder, OrchestratorInstructionsFilename);
        try
        {
            logger.LogDebug("Loading orchestrator system instructions from {InstructionPath}", instructionsPath);
            var baseInstructions = await GetSystemContextAsync(instructionsPath, cancellationToken);
            if (string.IsNullOrWhiteSpace(baseInstructions))
            {
                logger.LogWarning("Orchestrator system instructions at {InstructionPath} are empty.", instructionsPath);
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
            logger.LogError(ex, "Failed to load orchestrator system instructions from {InstructionPath}", instructionsPath);
            throw;
        }
    }

    private async Task<string> LoadDomainDescriptionsAsync(CancellationToken cancellationToken)
    {
        var descriptionsBuilder = new StringBuilder();

        foreach (var domain in domainSettings)
        {
            var descriptionPath = Path.Combine(ContextsFolder, DomainsFolder, domain.Key, DomainDescriptionFilename);
            logger.LogDebug("Loading domain description for {DomainKey} from {DescriptionPath}", domain.Key, descriptionPath);
            
            var description = await GetSystemContextAsync(descriptionPath, cancellationToken);
            if (string.IsNullOrWhiteSpace(description))
            {
                logger.LogError("Domain description for {DomainKey} at {DescriptionPath} is empty.", domain.Key, descriptionPath);
                throw new InvalidOperationException($"Domain {domain.Key} description is empty.");
            }
            
            descriptionsBuilder.AppendLine($"## {domain.ToolName}");
            descriptionsBuilder.AppendLine(description.Trim());
            descriptionsBuilder.AppendLine();
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
