using Microsoft.Agents.AI;

namespace ChatBro.AiService.Services
{
    public class ChatService(
        AIAgent chatAgent,
        IAgentThreadStore threadStore,
        ILogger<ChatService> logger
    )
    {
        public async Task<string> GetChatResponseAsync(string message, string userId)
        {
            var thread = await threadStore.GetThreadAsync(userId);

            logger.LogInformation("Sending chat request for user {UserId}", userId);
            var response = await chatAgent.RunAsync(message, thread);
            logger.LogInformation("Received chat response for user {UserId}, metadata {Metadata}", userId, response.AdditionalProperties);
            if (string.IsNullOrWhiteSpace(response.Text))
            {
                throw new InvalidOperationException("No text response from the model!");
            }

            await threadStore.SaveThreadAsync(userId, thread);

            return response.Text;
        }
    }
}