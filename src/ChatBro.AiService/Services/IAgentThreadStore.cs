using Microsoft.Agents.AI;

namespace ChatBro.AiService.Services;

public interface IAgentThreadStore
{
    /// <summary>
    /// Gets or creates an AgentThread for the specified session (userId).
    /// </summary>
    Task<AgentThread> GetThreadAsync(string userId);

    /// <summary>
    /// Saves the AgentThread state for the specified session (userId).
    /// </summary>
    Task SaveThreadAsync(string userId, AgentThread thread);
}
