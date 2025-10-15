namespace ChatBro.RestaurantsService.Configuration;

public class SchedulerSettings
{
    public string WarmupJobCron { get; init; } = "0 6 * * 1-5";
}
