using ChatBro.AiService.Services;
using Microsoft.Agents.AI;

namespace ChatBro.AiService.Providers;

public class InstructionsAIContextProvider(IContextProvider contextProvider, ILogger<InstructionsAIContextProvider> logger) : AIContextProvider
{
    public override async ValueTask<AIContext> InvokingAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var instructions = await contextProvider.GetSystemContextAsync();
            if (string.IsNullOrWhiteSpace(instructions))
            {
                logger.LogWarning("System instructions are empty.");
                return new AIContext();
            }

            var aiContext = new AIContext()
            {
                Instructions = instructions
            };
            return aiContext;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load system instructions from IContextProvider");
            throw;
        }
    }
}
