using System.Text.Json;
using ChatBro.ServiceDefaults;
using StackExchange.Redis;

namespace ChatBro.Server.Services.AI.Memory;

/// <summary>
/// Redis-backed implementation of <see cref="IObservationalMemoryStore"/>.
/// Key format: chatbro:memory:{userId}, no TTL.
/// All operations emit OpenTelemetry spans via <see cref="MemoryActivitySource"/>.
/// </summary>
public class RedisObservationalMemoryStore(IConnectionMultiplexer redis) : IObservationalMemoryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private static string Key(string userId) => $"chatbro:memory:{userId}";

    public async Task<UserMemory> LoadAsync(string userId, CancellationToken cancellationToken = default)
    {
        using var activity = MemoryActivitySource.Source.StartActivity(MemoryActivitySource.SpanNames.Load);
        activity?.SetTag(MemoryActivitySource.TagKeys.UserId, userId);

        try
        {
            var db = redis.GetDatabase();
            var value = await db.StringGetAsync(Key(userId));

            UserMemory memory;
            if (value.IsNullOrEmpty)
            {
                memory = new UserMemory();
            }
            else
            {
                memory = JsonSerializer.Deserialize<UserMemory>(value.ToString(), JsonOptions) ?? new UserMemory();
            }

            activity?.SetTag(MemoryActivitySource.TagKeys.ObservationsCount, memory.Observations.Count);
            activity?.SetTag(MemoryActivitySource.TagKeys.RawMessagesCount, memory.RawMessages.Count);

            return memory;
        }
        catch (Exception ex)
        {
            activity?.SetException(ex);
            throw;
        }
    }

    public async Task SaveAsync(string userId, UserMemory memory, CancellationToken cancellationToken = default)
    {
        using var activity = MemoryActivitySource.Source.StartActivity(MemoryActivitySource.SpanNames.Save);
        activity?.SetTag(MemoryActivitySource.TagKeys.UserId, userId);

        try
        {
            activity?.SetTag(MemoryActivitySource.TagKeys.ObservationsCount, memory.Observations.Count);
            activity?.SetTag(MemoryActivitySource.TagKeys.RawMessagesCount, memory.RawMessages.Count);

            var db = redis.GetDatabase();
            var json = JsonSerializer.Serialize(memory, JsonOptions);
            await db.StringSetAsync(Key(userId), json);
        }
        catch (Exception ex)
        {
            activity?.SetException(ex);
            throw;
        }
    }

    public async Task DeleteAsync(string userId, CancellationToken cancellationToken = default)
    {
        using var activity = MemoryActivitySource.Source.StartActivity(MemoryActivitySource.SpanNames.Delete);
        activity?.SetTag(MemoryActivitySource.TagKeys.UserId, userId);

        try
        {
            var db = redis.GetDatabase();
            await db.KeyDeleteAsync(Key(userId));

            activity?.SetTag(MemoryActivitySource.TagKeys.ObservationsCount, 0);
            activity?.SetTag(MemoryActivitySource.TagKeys.RawMessagesCount, 0);
        }
        catch (Exception ex)
        {
            activity?.SetException(ex);
            throw;
        }
    }
}
