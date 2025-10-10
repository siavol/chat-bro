using Coravel.Invocable;
using ChatBro.RestaurantsService.Clients;

namespace ChatBro.RestaurantsService.Jobs;

public class WarmupLounaatCacheJob(
    LounaatClient client,
    ILogger<WarmupLounaatCacheJob> logger) : IInvocable
{
    public async Task Invoke()
    {
        try
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            logger.LogInformation("Warmup job: fetching restaurants for {Date}", today);
            var restaurants = await client.GetRestaurants(today);
            logger.LogInformation("Warmup job: fetched {Count} restaurants for {Date}", restaurants?.Count ?? 0, today);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Warmup job failed");
        }
    }
}
