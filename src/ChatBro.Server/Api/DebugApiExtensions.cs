using System.Diagnostics;
using ChatBro.Server.Services.AI;
using ChatBro.Server.Services.Telegram;

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

        app.MapPost("/debug/command/{name}", async (
                string name,
                DebugCommandRequest? request,
                IEnumerable<ITelegramCommand> commands,
                IServiceScopeFactory scopeFactory) =>
            {
                var command = commands.FirstOrDefault(c =>
                    string.Equals(c.Command, name, StringComparison.OrdinalIgnoreCase));

                if (command is null)
                {
                    return Results.NotFound(new { error = $"Command '{name}' not found" });
                }

                var userId = string.IsNullOrWhiteSpace(request?.UserId) ? "debug" : request.UserId;
                using var scope = scopeFactory.CreateScope();
                var result = await command.ExecuteAsync(scope, userId);

                var activity = Activity.Current;
                return Results.Ok(new
                {
                    command = name,
                    result,
                    traceId = activity?.TraceId.ToString(),
                    spanId = activity?.SpanId.ToString()
                });
            })
            .WithName("DebugCommand");

        return app;
    }

    internal sealed class DebugChatRequest
    {
        public string Message { get; set; } = string.Empty;
        public string? UserId { get; set; }
    }

    internal sealed class DebugCommandRequest
    {
        public string? UserId { get; set; }
    }
}
