using Microsoft.Agents.AI;

namespace ChatBro.Server.Services
{
    public class ChatService(
        IAIAgentProvider agentProvider,
        IAgentThreadStore threadStore,
        ILogger<ChatService> logger
    )
    {
        public async Task<string> GetChatResponseAsync(string message, string userId)
        {
            var chatAgent = await agentProvider.GetAgentAsync();
            var thread = await threadStore.GetThreadAsync(userId, chatAgent);

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

        public async Task<bool> ResetChatAsync(string userId)
        {
            logger.LogInformation("Resetting chat thread for user {UserId}", userId);
            return await threadStore.DeleteThreadAsync(userId);
        }
    }
}
