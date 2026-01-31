using System.Text.Json;
using Microsoft.Agents.AI;
using StackExchange.Redis;

namespace ChatBro.Server.Services.AI;

public class InMemoryAgentSessionStore(
    IConnectionMultiplexer redis,
    ILogger<InMemoryAgentSessionStore> logger)
    : IAgentSessionStore
{
    private readonly JsonSerializerOptions JsonOptions = JsonSerializerOptions.Web;
    private readonly TimeSpan DefaultTtl = TimeSpan.FromDays(7);

    public async Task<AgentSession> GetThreadAsync(string userId, AIAgent agent)
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
                    var session = await agent.GetNewSessionAsync();
                    return session;
                }
                var restoredSession = await agent.DeserializeSessionAsync(state.Json);
                logger.LogDebug("Loaded thread for user {UserId} from Redis", userId);
                return restoredSession;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load/deserialize thread for user {UserId}, creating a new thread", userId);
        }

        return await agent.GetNewSessionAsync();
    }

    public async Task SaveThreadAsync(string userId, AgentSession thread)
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

    public async Task<bool> DeleteThreadAsync(string userId)
    {
        var db = redis.GetDatabase();
        var key = BuildKey(userId);
        try
        {
            var deleted = await db.KeyDeleteAsync(key);
            if (deleted)
            {
                logger.LogInformation("Deleted thread for user {UserId} from Redis", userId);
            }
            else
            {
                logger.LogDebug("No thread found to delete for user {UserId}", userId);
            }

            return deleted;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete thread for user {UserId}", userId);
            throw;
        }
    }
        
    private static string BuildKey(string userId) => $"chatbro:thread:{userId}";
    
    private sealed record ThreadState(JsonElement Json);
}

