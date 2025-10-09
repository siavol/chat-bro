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

        var responseText = await chatService.GetChatResponseAsync(request.Message);
        return Ok(new ChatResponse(responseText));
    }

    public record ChatRequest(string Message);

    public record ChatResponse(string TextContent);
}