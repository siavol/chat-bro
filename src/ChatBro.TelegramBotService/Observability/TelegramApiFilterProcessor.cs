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
            var statusCode = activity.GetTagItem("http.response.status_code") switch
            {
                int i => i.ToString(),
                string s => s,
                _ => null
            };

            if (destination == "api.telegram.org"
                && method == "POST"
                && statusCode == "200"
                && activity.GetTagItem("url.full") is string url
                && url.EndsWith("/getUpdates", StringComparison.OrdinalIgnoreCase)
                && activity.Status != ActivityStatusCode.Error)
            {
                activity.Ignore();
            }
        }
    }
}