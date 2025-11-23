using System.Collections.Generic;
using System.Threading;
using Microsoft.Agents.AI;

namespace ChatBro.Server.Services;

public interface IAIAgentProvider
{
    Task<AIAgent> GetAgentAsync();

    Task<IReadOnlyList<DomainAgentRegistration>> GetDomainAgentsAsync(CancellationToken cancellationToken = default);
}

