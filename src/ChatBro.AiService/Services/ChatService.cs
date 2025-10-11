using System.Diagnostics;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using TextContent = Microsoft.SemanticKernel.TextContent;

namespace ChatBro.AiService.Services
{
    public class ChatService(
        IOptions<DependencyInjection.ChatHistoryOptions> historyOptions,
        Kernel kernel,
        IContextProvider contextProvider,
        IMemoryCache cache,
        ILogger<ChatService> logger
    )
    {
        private readonly IChatCompletionService _chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
        private readonly DependencyInjection.ChatHistoryOptions _historyOptions = historyOptions.Value;

        public async Task<string> GetChatResponseAsync(string message)
        {
            PromptExecutionSettings promptExecutionSettings = new()
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };

            var history = new ChatHistory();


            history.AddUserMessage(message);

            logger.LogInformation("Sending chat request");
            var result = await _chatCompletion.GetChatMessageContentAsync(history, promptExecutionSettings, kernel);
            logger.LogInformation("Received chat response: {Metadata}", result.Metadata);
            AddChangeResponseReceivedEvent(result);

            var responseItem = result.Items.OfType<TextContent>().Single();
            var responseText = responseItem.Text!;
            if (string.IsNullOrWhiteSpace(responseText))
            {
                throw new InvalidOperationException("No text response from the model!");
            }

            return responseText;
        }

        private async Task<ChatSessionState> GetOrCreateSessionAsync(string key)
        {
            // Build cache entry options from settings
            var entryOptions = new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(_historyOptions.SlidingExpirationMinutes),
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(_historyOptions.AbsoluteExpirationHours)
            };

            var state = await cache.GetOrCreateAsync(key, async cacheEntry =>
            {
                cacheEntry.SetOptions(entryOptions);

                var newState = new ChatSessionState();
                var systemContext = await contextProvider.GetSystemContextAsync();
                if (!string.IsNullOrWhiteSpace(systemContext))
                {
                    // Add system context to the chat history so the model receives processing rules and expectations
                    newState.History.AddSystemMessage(systemContext);
                }

                return newState;
            });

            return state ?? throw new InvalidOperationException("Failed to create or retrieve chat session state.");
        }

        private static void AddChangeResponseReceivedEvent(ChatMessageContent result)
        {
            var eventTags = new ActivityTagsCollection();
            if (result.Metadata != null &&
                result.Metadata.TryGetValue("Usage", out var usage) &&
                usage is UsageDetails usageDetails)
            {
                eventTags.Add("Usage.InputTokenCount", usageDetails.InputTokenCount);
                eventTags.Add("Usage.OutputTokenCount", usageDetails.OutputTokenCount);
                eventTags.Add("Usage.TotalTokenCount", usageDetails.TotalTokenCount);
            }
            ActivityEvent e = new("ChatResponseReceived", tags: eventTags);
            Activity.Current?.AddEvent(e);
        }

        private sealed class ChatSessionState
        {
            public ChatHistory History { get; } = new();
            public DateTime LastActivityUtc { get; set; } = DateTime.UtcNow;
            public int MessageCount { get; set; }
            public SemaphoreSlim Lock { get; } = new(1, 1);
        }
    }
}