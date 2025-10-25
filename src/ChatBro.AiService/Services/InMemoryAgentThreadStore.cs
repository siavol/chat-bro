using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Caching.Memory;

namespace ChatBro.AiService.Services;

public class InMemoryAgentThreadStore(AIAgent agent, IMemoryCache cache, ILogger<InMemoryAgentThreadStore> logger) : IAgentThreadStore
{
    private readonly JsonSerializerOptions JsonOptions = JsonSerializerOptions.Default;
    private readonly TimeSpan DefaultTtl = TimeSpan.FromDays(7);

    public Task<AgentThread> GetThreadAsync(string userId)
    {
        var key = new CacheKey(userId);
        if (cache.TryGetValue<ThreadState>(key, out var state) && state != null)
        {
            try
            {
                var jsonElement = state.Json;
                var thread = agent.DeserializeThread(jsonElement);
                return Task.FromResult(thread);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to deserialize thread for user {UserId}, creating a new thread", userId);
                return Task.FromResult(agent.GetNewThread());
            }
        }

        return Task.FromResult(agent.GetNewThread());
    }

    public Task SaveThreadAsync(string userId, AgentThread thread)
    {
        var key = new CacheKey(userId);
        try
        {
            var jsonElement = thread.Serialize(JsonOptions);
            var state = new ThreadState(jsonElement);
            cache.Set(key, state, DefaultTtl);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to serialize thread for user {UserId}", userId);
        }

        return Task.CompletedTask;
    }

    private sealed record CacheKey(string SessionId);
    private sealed record ThreadState(JsonElement Json);
}
