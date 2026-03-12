using System.Text;
using ChatBro.Server.Services.AI.Memory;

namespace ChatBro.Server.Services.Telegram;

public class ShowMemoryCommand : ITelegramCommand
{
    public string Command => "show_memory";
    public string Description => "Show observational memory stored for you";

    public async Task<string> ExecuteAsync(IServiceScope scope, string userId)
    {
        var store = scope.ServiceProvider.GetRequiredService<IObservationalMemoryStore>();
        var memory = await store.LoadAsync(userId);

        if (memory.Observations.Count == 0 && memory.RawMessages.Count == 0)
            return "No observational memory stored.";

        var sb = new StringBuilder();

        if (memory.Observations.Count > 0)
        {
            sb.AppendLine($"📝 **Observations** ({memory.Observations.Count}):");
            foreach (var obs in memory.Observations)
            {
                var emoji = obs.Importance switch
                {
                    "high" => "🔴",
                    "medium" => "🟡",
                    "low" => "🟢",
                    _ => "⚪"
                };
                sb.AppendLine($"  {emoji} [{obs.Timestamp:yyyy-MM-dd}] {obs.Text}");
            }
        }

        if (memory.RawMessages.Count > 0)
        {
            if (sb.Length > 0) sb.AppendLine();
            sb.AppendLine($"💬 **Unprocessed messages**: {memory.RawMessages.Count}");
        }

        return sb.ToString().TrimEnd();
    }
}
