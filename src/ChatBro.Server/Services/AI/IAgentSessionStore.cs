using Microsoft.Agents.AI;

namespace ChatBro.Server.Services.AI;

public interface IAgentSessionStore
{
    /// <summary>
    /// Gets or creates an AgentSession for the specified session (userId).
    /// </summary>
    Task<AgentSession> GetThreadAsync(string userId, AIAgent agent);

    /// <summary>
    /// Saves the AgentSession state for the specified session (userId).
    /// </summary>
    Task SaveThreadAsync(string userId, AgentSession thread);

    /// <summary>
    /// Deletes any persisted AgentSession state for the specified session (userId).
    /// </summary>
    /// <returns>True if a thread was deleted, false if no thread existed for the user.</returns>
    Task<bool> DeleteThreadAsync(string userId);
}
