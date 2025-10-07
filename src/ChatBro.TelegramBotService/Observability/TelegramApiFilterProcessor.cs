using System.Diagnostics;
using OpenTelemetry;

namespace ChatBro.TelegramBotService.Observability
{
    public class TelegramApiFilterProcessor : BaseProcessor<Activity>
    {
        public override void OnEnd(Activity activity)
        {
            var destination = activity.GetTagItem("server.address") as string;
            var method = activity.GetTagItem("http.request.method") as string;
            var urlTag = activity.GetTagItem("url.full");
            var statusCode = activity.GetTagItem("http.response.status_code") switch
            {
                int i => i.ToString(),
                string s => s,
                _ => null
            };
            var hasError = activity.Status == ActivityStatusCode.Error;

            // Adjust the path as needed
            if (destination == "api.telegram.org"
                && method == "POST"
                && statusCode == "200"
                && urlTag is string url && url.EndsWith("/getUpdates", StringComparison.OrdinalIgnoreCase)
                && !hasError)
            {
                activity.Ignore();
            }
        }
    }
}