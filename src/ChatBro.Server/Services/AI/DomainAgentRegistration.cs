using Microsoft.Agents.AI;
using static ChatBro.Server.Options.ChatSettings;

namespace ChatBro.Server.Services.AI;

/// <summary>
/// Describes a domain-specific agent that can be exposed as a tool to the orchestrator.
/// </summary>
public sealed record DomainAgentRegistration(
    string Key,
    string ToolName,
    string Description,
    AIAgent Agent)
{
    public static DomainAgentRegistration Create(
        DomainSettings settings,
        AIAgent agent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.Key);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.ToolName);
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.Description);

        return new DomainAgentRegistration(settings.Key, settings.ToolName, settings.Description, agent);
    }
}
