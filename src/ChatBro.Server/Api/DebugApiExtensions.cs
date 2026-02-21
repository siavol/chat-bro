using System.Diagnostics;
using ChatBro.Server.Services.AI;

namespace ChatBro.Server.Api;

internal static class DebugApiExtensions
{
    public static WebApplication MapDebugApi(this WebApplication app)
    {
        app.MapPost("/debug/chat", async (DebugChatRequest request, ChatService chatService) =>
            {
                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    return Results.BadRequest(new { error = "message is required" });
                }

                var userId = string.IsNullOrWhiteSpace(request.UserId) ? "debug" : request.UserId;
                var reply = await chatService.GetChatResponseAsync(request.Message, userId);

                var activity = Activity.Current;
                return Results.Ok(new
                {
                    reply,
                    traceId = activity?.TraceId.ToString(),
                    spanId = activity?.SpanId.ToString()
                });
            })
            .WithName("DebugChat");

        return app;
    }

    internal sealed class DebugChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public string? UserId { get; set; }
    }
}
