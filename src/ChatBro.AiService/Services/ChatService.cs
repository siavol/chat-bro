using ChatBro.AiService.Options;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ChatBro.AiService.Services
{
    public class ChatService(
        // IOptions<ChatHistorySettings> historyOptions,
        AIAgent chatAgent,
        IContextProvider contextProvider,
        IMemoryCache cache,
        ILogger<ChatService> logger
    )
    {
        // private readonly ChatHistorySettings _historyOptions = historyOptions.Value;

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

            // Append the incoming user message to the session history
            chatMessages.Add(new ChatMessage(ChatRole.User, message));

            logger.LogInformation("Sending chat request for user {UserId}", userId);
            var response = await chatAgent.RunAsync(chatMessages, state.Thread);
            logger.LogInformation("Received chat response for user {UserId}: {Metadata}", userId, response.AdditionalProperties);
            if (string.IsNullOrWhiteSpace(response.Text))
            {
                throw new InvalidOperationException("No text response from the model!");
            }

            return response.Text;
        }

        private async Task<ChatSessionState> GetOrCreateSessionAsync(string userId)
        {
            var key = new CacheKey(userId);
            var state = await cache.GetOrCreateAsync(key, async cacheEntry =>
            {
                var thread = chatAgent.GetNewThread();
                var newState = new ChatSessionState(thread);
                return newState;
            });

            return state ?? throw new InvalidOperationException("Failed to create or retrieve chat session state.");
        }

        private sealed record CacheKey(string SessionId);

        private sealed record ChatSessionState(AgentThread Thread)
        {
            public bool IsContextSet { get; set; } = false;
        }
    }
}