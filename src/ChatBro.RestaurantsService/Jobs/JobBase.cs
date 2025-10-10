using System.Diagnostics;
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
            // mark activity as error and attach exception details
            if (activity != null)
            {
                activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity.AddEvent(new ActivityEvent(
                    "exception",
                    tags: new ActivityTagsCollection
                    {
                        { "exception.type", ex.GetType().FullName },
                        { "exception.message", ex.Message },
                        { "exception.stacktrace", ex.ToString() }
                    }));
            }

            Logger.LogError(ex, "Job {Job} failed", typeof(T).Name);
        }
    }

    protected abstract Task ExecuteAsync();
}
