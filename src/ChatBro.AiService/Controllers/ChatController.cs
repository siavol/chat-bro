using System.Diagnostics;
using ChatBro.AiService.Services;
using Microsoft.AspNetCore.Mvc;

namespace ChatBro.AiService.Controllers;

[ApiController]
[Route("chat")]
public class ChatController(ChatService chatService) : ControllerBase
{
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

        Activity.Current?.SetTag("chat.user.id", request.UserId);
        var responseText = await chatService.GetChatResponseAsync(request.Message, request.UserId);
        return Ok(new ChatResponse(responseText));
    }

    public record ChatRequest(string UserId, string Message);

    public record ChatResponse(string TextContent);
}