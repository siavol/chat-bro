using Microsoft.Agents.AI;

namespace ChatBro.Server.Services.AI;

public interface IAIAgentProvider
{
    Task<AIAgent> GetAgentAsync();

    Task<IReadOnlyList<DomainAgentRegistration>> GetDomainAgentsAsync(CancellationToken cancellationToken = default);
}

