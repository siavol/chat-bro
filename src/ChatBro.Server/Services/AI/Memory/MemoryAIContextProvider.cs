using System.Text;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ChatBro.Server.Services.AI.Memory;

/// <summary>
/// Injects observational memory (observations + recent raw messages) into agent prompts
/// by reading from <see cref="ObservationalMemoryContext.Current"/>.
/// A single instance is shared across all agents (orchestrator + domain agents).
/// </summary>
public sealed class MemoryAIContextProvider(ObservationalMemoryContext memoryContext) : AIContextProvider
{
    protected override ValueTask<AIContext> ProvideAIContextAsync(
        InvokingContext context, CancellationToken cancellationToken = default)
    {
        var memory = memoryContext.Current;
        if (memory is null)
        {
            return ValueTask.FromResult(new AIContext());
        }

        var hasObservations = memory.Observations.Count > 0;
        var hasRawMessages = memory.RawMessages.Count > 0;

        if (!hasObservations && !hasRawMessages)
        {
            return ValueTask.FromResult(new AIContext());
        }

        var sb = new StringBuilder();
        sb.AppendLine("## Observational Memory");
        sb.AppendLine();

        if (hasObservations)
        {
            sb.AppendLine("### Observations");
            foreach (var obs in memory.Observations)
            {
                sb.AppendLine($"- [{obs.Importance}] {obs.Text} (recorded {obs.Timestamp:yyyy-MM-dd})");
            }
            sb.AppendLine();
        }

        if (hasRawMessages)
        {
            sb.AppendLine("### Recent Unprocessed Messages");
            foreach (var msg in memory.RawMessages)
            {
                sb.AppendLine($"- **User** ({msg.Timestamp:yyyy-MM-dd HH:mm}): {msg.UserMessage}");
                sb.AppendLine($"  **Assistant**: {msg.AssistantResponse}");
            }
            sb.AppendLine();
        }

        var aiContext = new AIContext
        {
            Messages = [new ChatMessage(ChatRole.System, sb.ToString())]
        };

        return ValueTask.FromResult(aiContext);
    }
}
