using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace ChatBro.AiService.Controllers;

[ApiController]
[Route("chat")]
public class ChatController(Kernel kernel, ILogger<ChatController> logger) : ControllerBase
{
    private readonly IChatCompletionService _chatCompletion = kernel.GetRequiredService<IChatCompletionService>();

    [HttpPost]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        if (request == null)
        {
            return BadRequest("Chat request cannot be null.");
        }
        if (string.IsNullOrEmpty(request.Message))
        {
            return BadRequest("Chat request Message can not be null or empty.");
        }

        PromptExecutionSettings promptExecutionSettings = new()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var history = new ChatHistory();
        history.AddUserMessage(request.Message);

        logger.LogInformation("Sending chat request");
        var result = await _chatCompletion.GetChatMessageContentAsync(history, promptExecutionSettings, kernel);
        logger.LogInformation("Received chat response: {Metadata}", result.Metadata);
        AddChangeResponseReceivedEvent(result);

        return Ok(new
        {
            result
        });
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
    public record ChatRequest(string Message);
}