using ChatBro.Server.DependencyInjection;
using ChatBro.RestaurantsService.KernelFunction;
using ChatBro.Server.Services.AI;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddRedisClient(connectionName: "redis");

builder.Services
    .AddScoped<ChatService>()
    .AddControllers();

builder.Services.AddHttpClient<RestaurantsServiceClient>(
    static client => client.BaseAddress = new Uri("https+http://chatbro-restaurants"));

builder.AddAgents();
builder.AddTelegramBot();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

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
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

internal sealed class DebugChatRequest
{
    public string Message { get; set; } = string.Empty;
    public string? UserId { get; set; }
}

