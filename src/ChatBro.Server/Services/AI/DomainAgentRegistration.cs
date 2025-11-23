using Microsoft.Agents.AI;

namespace ChatBro.Server.Services.AI;

/// <summary>
/// Describes a domain-specific agent that can be exposed as a tool to the orchestrator.
/// </summary>
public sealed record DomainAgentRegistration(
    string Key,
    string ToolName,
    string Description,
    AIAgent Agent);
