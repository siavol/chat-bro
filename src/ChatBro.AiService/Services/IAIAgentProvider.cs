using Microsoft.Agents.AI;

namespace ChatBro.AiService.Services;

public interface IAIAgentProvider
{
    Task<AIAgent> GetAgentAsync();
}
