using System.Diagnostics;
using ChatBro.ServiceDefaults;
namespace ChatBro.RestaurantsService.Jobs;

public abstract class JobBase<T>(ILogger<T> logger, ActivitySource activitySource) : Coravel.Invocable.IInvocable where T : class
{
    protected readonly ILogger<T> Logger = logger;

    public async Task Invoke()
    {
        using var activity = activitySource.StartActivity(typeof(T).Name, ActivityKind.Internal);
        try
        {
            await ExecuteAsync();
        }
        catch (Exception ex)
        {
            activity.SetException(ex);
            Logger.LogError(ex, "Job {Job} failed", typeof(T).Name);
        }
    }

    protected abstract Task ExecuteAsync();
}
