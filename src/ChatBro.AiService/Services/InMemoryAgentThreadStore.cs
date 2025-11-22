using System.Text.Json;
using Microsoft.Agents.AI;
using StackExchange.Redis;

namespace ChatBro.AiService.Services;

public class InMemoryAgentThreadStore(
    IConnectionMultiplexer redis,
    ILogger<InMemoryAgentThreadStore> logger)
    : IAgentThreadStore
{
    private readonly JsonSerializerOptions JsonOptions = JsonSerializerOptions.Web;
    private readonly TimeSpan DefaultTtl = TimeSpan.FromDays(7);

    public async Task<AgentThread> GetThreadAsync(string userId, AIAgent agent)
    {
        var db = redis.GetDatabase();
        var key = BuildKey(userId);
        try
        {
            var json = await db.StringGetAsync(key);
            if (json.HasValue)
            {
                var state = JsonSerializer.Deserialize<ThreadState>(json.ToString(), JsonOptions);
                if (state == null || state.Json.ValueKind == JsonValueKind.Undefined)
                {
                    logger.LogWarning("Deserialized null or invalid state for user {UserId}", userId);
                    return agent.GetNewThread();
                }
                var thread = agent.DeserializeThread(state.Json);
                logger.LogDebug("Loaded thread for user {UserId} from Redis", userId);
                return thread;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load/deserialize thread for user {UserId}, creating a new thread", userId);
        }

        return agent.GetNewThread();
    }

    public async Task SaveThreadAsync(string userId, AgentThread thread)
    {
        var db = redis.GetDatabase();
        var key = BuildKey(userId);
        try
        {
            var element = thread.Serialize(JsonOptions);
            var state = new ThreadState(element);
            var json = JsonSerializer.Serialize(state, JsonOptions);
            await db.StringSetAsync(key, json, DefaultTtl);
            logger.LogDebug("Saved thread for user {UserId} to Redis", userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to serialize thread for user {UserId}", userId);
        }
    }
        
    private static string BuildKey(string userId) => $"chatbro:thread:{userId}";
    
    private sealed record ThreadState(JsonElement Json);
}
