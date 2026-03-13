using ChatBro.Server.Options;
using ChatBro.Server.Services.AI.Memory;
using Microsoft.Extensions.Options;

namespace ChatBro.Server.Services.AI
{
    public class ChatService(
        IAIAgentProvider agentProvider,
        IAgentSessionStore sessionStore,
        IDomainToolingBuilder domainToolingBuilder,
        IObservationalMemoryStore memoryStore,
        ObservationalMemoryContext memoryContext,
        IObserverService observerService,
        IReflectorService reflectorService,
        IOptions<ObservationalMemorySettings> memorySettings,
        ILogger<ChatService> logger
    )
    {
        public async Task<string> GetChatResponseAsync(string message, string userId)
        {
            var chatAgent = await agentProvider.GetAgentAsync();
            var thread = await sessionStore.GetThreadAsync(userId, chatAgent);
            var domainTooling = await domainToolingBuilder.CreateAsync(userId);

            // Load observational memory and set AsyncLocal context for MemoryAIContextProvider
            var memory = await memoryStore.LoadAsync(userId);
            memoryContext.Current = memory;

            try
            {
                logger.LogInformation("Sending chat request for user {UserId}", userId);
                var response = await chatAgent.RunAsync(message, thread, domainTooling.RunOptions);
                logger.LogInformation("Received chat response for user {UserId}, metadata {Metadata}", userId, response.AdditionalProperties);
                if (string.IsNullOrWhiteSpace(response.Text))
                {
                    throw new InvalidOperationException("No text response from the model!");
                }

                // Capture the raw message exchange for observational memory
                memory ??= new UserMemory();
                memory.RawMessages.Add(new RawMessage
                {
                    Timestamp = DateTimeOffset.UtcNow,
                    UserMessage = message,
                    AssistantResponse = response.Text
                });
                await memoryStore.SaveAsync(userId, memory);

                // Trigger observer if raw message count exceeds threshold
                var settings = memorySettings.Value;
                if (memory.RawMessages.Count >= settings.ObserverRawMessageThreshold)
                {
                    try
                    {
                        memory = await observerService.ObserveAsync(memory);
                        await memoryStore.SaveAsync(userId, memory);

                        // Trigger reflector if observation count exceeds threshold
                        if (memory.Observations.Count >= settings.ReflectorObservationThreshold)
                        {
                            try
                            {
                                memory = await reflectorService.ReflectAsync(memory);
                                await memoryStore.SaveAsync(userId, memory);
                            }
                            catch (Exception ex)
                            {
                                logger.LogWarning(ex, "Reflector failed for user {UserId}; observations preserved", userId);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Observer failed for user {UserId}; raw messages preserved", userId);
                    }
                }

                await sessionStore.SaveThreadAsync(userId, chatAgent, thread);
                foreach (var domainThread in domainTooling.DomainThreads)
                {
                    await sessionStore.SaveThreadAsync(domainThread.ThreadKey, domainThread.Agent, domainThread.Thread);
                }

                return response.Text;
            }
            finally
            {
                memoryContext.Current = null;
            }
        }

        public async Task<bool> ResetChatAsync(string userId)
        {
            logger.LogInformation("Resetting chat thread for user {UserId}", userId);
            var orchestratorReset = await sessionStore.DeleteThreadAsync(userId);
            var domainReset = await domainToolingBuilder.ResetAsync(userId);
            return orchestratorReset || domainReset;
        }

        public async Task<bool> HardResetChatAsync(string userId)
        {
            logger.LogInformation("Hard resetting chat and memory for user {UserId}", userId);
            var chatReset = await ResetChatAsync(userId);
            await memoryStore.DeleteAsync(userId);
            return chatReset;
        }
    }
}
