using System.Diagnostics;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using TextContent = Microsoft.SemanticKernel.TextContent;

namespace ChatBro.AiService.Services
{
    public class ChatService(
        Kernel kernel,
        IContextProvider contextProvider,
        ILogger<ChatService> logger
    )
    {
        private readonly IChatCompletionService _chatCompletion = kernel.GetRequiredService<IChatCompletionService>();

        public async Task<string> GetChatResponseAsync(string message)
        {
            PromptExecutionSettings promptExecutionSettings = new()
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            };

            var history = new ChatHistory();

            var systemContext = await contextProvider.GetSystemContextAsync();
            if (!string.IsNullOrWhiteSpace(systemContext))
            {
                // Add system context to the chat history so the model receives processing rules and expectations
                history.AddSystemMessage(systemContext);
            }

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
    }
}