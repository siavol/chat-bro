using System.Diagnostics;
using ChatBro.AiService.Options;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ChatBro.AiService.Services
{
    public class ChatService(
        IOptions<ChatHistorySettings> historyOptions,
        AIAgent chatAgent,
        IContextProvider contextProvider,
        IMemoryCache cache,
        ILogger<ChatService> logger
    )
    {
        // private readonly IChatCompletionService _chatCompletion = kernel.GetRequiredService<IChatCompletionService>();
        private readonly ChatHistorySettings _historyOptions = historyOptions.Value;

        public async Task<string> GetChatResponseAsync(string message, string userId)
        {
            var state = await GetOrCreateSessionAsync(userId);

            var chatMessages = new List<ChatMessage>(2);
            if (state.IsContextSet == false)
            {
                var systemContext = await contextProvider.GetSystemContextAsync();
                if (!string.IsNullOrWhiteSpace(systemContext))
                {
                    // Add system context to the chat history so the model receives processing rules and expectations
                    chatMessages.Add(new ChatMessage(ChatRole.System, systemContext));
                }
            }

            // await state.Lock.WaitAsync();
            try
            {
                // Append the incoming user message to the session history
                chatMessages.Add(new ChatMessage(ChatRole.User, message));

                logger.LogInformation("Sending chat request for user {UserId}", userId);
                
                var response = await chatAgent.RunAsync(chatMessages, state.Thread);

                logger.LogInformation("Received chat response for user {UserId}: {Metadata}", userId, response.AdditionalProperties);
                AddChangeResponseReceivedEvent(response);

                var responseItem = response;
                // var responseItem = result.Items.OfType<TextContent>().Single();
                var responseText = responseItem.Text!;
                if (string.IsNullOrWhiteSpace(responseText))
                {
                    throw new InvalidOperationException("No text response from the model!");
                }

                return responseText;
            }
            finally
            {
                // state.Lock.Release();
            }
        }

        private async Task<ChatSessionState> GetOrCreateSessionAsync(string userId)
        {
            var key = new CacheKey(userId);

            // Build cache entry options from settings
            var entryOptions = new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(_historyOptions.SlidingExpirationMinutes),
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(_historyOptions.AbsoluteExpirationHours)
            };

            // Use the record instance itself as the cache key; this avoids accidental collisions with other services
            var state = await cache.GetOrCreateAsync(key, async cacheEntry =>
            {
                cacheEntry.SetOptions(entryOptions);

                var thread = chatAgent.GetNewThread();
                var newState = new ChatSessionState(thread);
                return newState;
            });

            return state ?? throw new InvalidOperationException("Failed to create or retrieve chat session state.");
        }

        private static void AddChangeResponseReceivedEvent(AgentRunResponse response)
        {
            // var eventTags = new ActivityTagsCollection();
            // if (result.Metadata != null &&
            //     result.Metadata.TryGetValue("Usage", out var usage) &&
            //     usage is UsageDetails usageDetails)
            // {
            //     eventTags.Add("Usage.InputTokenCount", usageDetails.InputTokenCount);
            //     eventTags.Add("Usage.OutputTokenCount", usageDetails.OutputTokenCount);
            //     eventTags.Add("Usage.TotalTokenCount", usageDetails.TotalTokenCount);
            // }
            // ActivityEvent e = new("ChatResponseReceived", tags: eventTags);
            // Activity.Current?.AddEvent(e);
        }

        private sealed record CacheKey(string SessionId);

        private sealed record ChatSessionState(AgentThread Thread)
        {
            public bool IsContextSet { get; set; } = false;

            // public ChatHistory History { get; } = new();
            // public DateTime LastActivityUtc { get; set; } = DateTime.UtcNow;
            // public SemaphoreSlim Lock { get; } = new(1, 1);
        }
    }
}