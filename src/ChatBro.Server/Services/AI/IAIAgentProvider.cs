using Microsoft.Agents.AI;

namespace ChatBro.Server.Services.AI;

/// <summary>  
/// Provides access to AI agents used by the application, including the central orchestrator  
/// agent and domain-specific agents.  
/// </summary>  
public interface IAIAgentProvider
{
    Task<AIAgent> GetAgentAsync();

    Task<IReadOnlyList<DomainAgentRegistration>> GetDomainAgentsAsync(CancellationToken cancellationToken = default);
}

