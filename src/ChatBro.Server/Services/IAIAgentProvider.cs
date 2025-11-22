using Microsoft.Agents.AI;

namespace ChatBro.Server.Services;

public interface IAIAgentProvider
{
    Task<AIAgent> GetAgentAsync();
}

