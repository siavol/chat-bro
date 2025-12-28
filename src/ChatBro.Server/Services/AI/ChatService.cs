namespace ChatBro.Server.Services.AI
{
    public class ChatService(
        IAIAgentProvider agentProvider,
        IAgentThreadStore threadStore,
        IDomainToolingBuilder domainToolingBuilder,
        ILogger<ChatService> logger
    )
    {
        public async Task<string> GetChatResponseAsync(string message, string userId)
        {
            var chatAgent = await agentProvider.GetAgentAsync();
            var thread = await threadStore.GetThreadAsync(userId, chatAgent);
            var domainTooling = await domainToolingBuilder.CreateAsync(userId);

            logger.LogInformation("Sending chat request for user {UserId}", userId);
            var response = await chatAgent.RunAsync(message, thread, domainTooling.RunOptions);
            logger.LogInformation("Received chat response for user {UserId}, metadata {Metadata}", userId, response.AdditionalProperties);
            if (string.IsNullOrWhiteSpace(response.Text))
            {
                throw new InvalidOperationException("No text response from the model!");
            }

            await threadStore.SaveThreadAsync(userId, thread);
            foreach (var domainThread in domainTooling.DomainThreads)
            {
                await threadStore.SaveThreadAsync(domainThread.ThreadKey, domainThread.Thread);
            }

            return response.Text;
        }

        public async Task<bool> ResetChatAsync(string userId)
        {
            logger.LogInformation("Resetting chat thread for user {UserId}", userId);
            var orchestratorReset = await threadStore.DeleteThreadAsync(userId);
            var domainReset = await domainToolingBuilder.ResetAsync(userId);
            return orchestratorReset || domainReset;
        }
    }
}
