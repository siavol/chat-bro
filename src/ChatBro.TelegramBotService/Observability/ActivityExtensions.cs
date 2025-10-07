
using System.Diagnostics;

namespace ChatBro.TelegramBotService.Observability;

public static class ActivityExtensions
{
    public static void SetException(this Activity? activity, Exception exception)
    {
        if (activity == null) return;
        activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        activity.AddEvent(new ActivityEvent(
            "exception",
            tags: new ActivityTagsCollection
            {
                { "exception.type", exception.GetType().FullName },
                { "exception.message", exception.Message },
                { "exception.stacktrace", exception.ToString() }
            }));
    }

    /// <summary>
    /// Marks the activity as ignored for export (clears Recorded flag).
    /// </summary>
    public static void Ignore(this Activity? activity)
    {
        if (activity == null) return;
        activity.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded;
    }
}

