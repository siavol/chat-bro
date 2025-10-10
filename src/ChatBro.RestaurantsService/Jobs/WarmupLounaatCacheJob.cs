using ChatBro.RestaurantsService.Clients;
using System.Diagnostics;

namespace ChatBro.RestaurantsService.Jobs;

public class WarmupLounaatCacheJob(
    LounaatClient client,
    ILogger<WarmupLounaatCacheJob> logger,
    ActivitySource activitySource) 
    : JobBase<WarmupLounaatCacheJob>(logger, activitySource)
{
    protected override async Task ExecuteAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        Logger.LogInformation("Warmup job: fetching restaurants for {Date}", today);
        var restaurants = await client.GetRestaurants(today, ignoreCache: true);
        Logger.LogInformation("Warmup job: fetched {Count} restaurants for {Date}", restaurants?.Count ?? 0, today);
    }
}
