using System.Text.Json;
using Microsoft.Agents.AI;
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
            var thread = GetOrCreateThread(userId);

            logger.LogInformation("Sending chat request for user {UserId}", userId);
            var response = await chatAgent.RunAsync(message, thread);
            logger.LogInformation("Received chat response for user {UserId}: {Metadata}", userId, response.AdditionalProperties);
            if (string.IsNullOrWhiteSpace(response.Text))
            {
                throw new InvalidOperationException("No text response from the model!");
            }

            var jsonThreadState = thread.Serialize(JsonSerializerOptions.Web);
            cache.Set(
                new CacheKey(userId),
                new ChatSessionState(jsonThreadState),
                TimeSpan.FromDays(3));

            return response.Text;
        }

        private AgentThread GetOrCreateThread(string userId)
        {
            var key = new CacheKey(userId);
            if (cache.TryGetValue<ChatSessionState>(key, out var existingState))
            {
                return chatAgent.DeserializeThread(existingState!.JsonThreadState);
            }

            return chatAgent.GetNewThread();
        }

        private sealed record CacheKey(string SessionId);

        private sealed record ChatSessionState(JsonElement JsonThreadState);
    }
}