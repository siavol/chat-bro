using Microsoft.Agents.AI;

namespace ChatBro.Server.Services.AI;

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
    /// <returns>True if a thread was deleted, false if no thread existed for the user.</returns>
    Task<bool> DeleteThreadAsync(string userId);
}

