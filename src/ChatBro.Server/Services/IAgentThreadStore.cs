using Microsoft.Agents.AI;

namespace ChatBro.Server.Services;

public interface IAgentThreadStore
{
    /// <summary>
    /// Gets or creates an AgentThread for the specified session (userId).
    /// </summary>
    Task<AgentThread> GetThreadAsync(string userId, AIAgent agent);

    /// <summary>
    /// Saves the AgentThread state for the specified session (userId).
    /// </summary>
    Task SaveThreadAsync(string userId, AgentThread thread);

    /// <summary>
    /// Deletes any persisted AgentThread state for the specified session (userId).
    /// </summary>
    Task<bool> DeleteThreadAsync(string userId);
}

