using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Memory;

namespace ChatBro.AiService.Services
{
    public class ChatService(
        AIAgent chatAgent,
        IMemoryCache cache,
        ILogger<ChatService> logger
    )
    {
        public async Task<string> GetChatResponseAsync(string message, string userId)
        {
            var state = GetOrCreateSessionAsync(userId);

            var chatMessages = new ChatMessage[]
            {
                new(ChatRole.User, message)
            };

            logger.LogInformation("Sending chat request for user {UserId}", userId);
            var response = await chatAgent.RunAsync(chatMessages, state.Thread);
            logger.LogInformation("Received chat response for user {UserId}: {Metadata}", userId, response.AdditionalProperties);
            if (string.IsNullOrWhiteSpace(response.Text))
            {
                throw new InvalidOperationException("No text response from the model!");
            }

            return response.Text;
        }

        private ChatSessionState GetOrCreateSessionAsync(string userId)
        {
            var key = new CacheKey(userId);
            var state = cache.GetOrCreate(key, cacheEntry =>
            {
                var thread = chatAgent.GetNewThread();
                var newState = new ChatSessionState(thread);
                return newState;
            });

            return state ?? throw new InvalidOperationException("Failed to create or retrieve chat session state.");
        }

        private sealed record CacheKey(string SessionId);

        private sealed record ChatSessionState(AgentThread Thread);
    }
}