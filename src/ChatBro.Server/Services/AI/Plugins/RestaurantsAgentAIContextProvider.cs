using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ChatBro.Server.Services.AI.Plugins;

public sealed class RestaurantsAgentAIContextProvider 
    : DomainAgentAIContextProvider
{
    private readonly IChatClient _chatClient;
    private InternalState _state;
    
    public RestaurantsAgentAIContextProvider(
        IChatClient chatClient,
        ILogger<RestaurantsAgentAIContextProvider> logger,
        string agentKey,
        JsonElement serializedState, 
        JsonSerializerOptions? jsonSerializerOptions = null)
        : base(logger, agentKey)
    {
        _chatClient = chatClient;

        try
        {
            _state = serializedState.ValueKind == JsonValueKind.Object ?
                serializedState.Deserialize<InternalState>(jsonSerializerOptions)! :
                new InternalState();
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, 
                "Failed to deserialize RestaurantsAgentAIContextProvider state for AgentKey: {AgentKey}. Initialize with empty state", agentKey);
            _state = new InternalState();
        }
    }

    public override async ValueTask InvokedAsync(InvokedContext context, CancellationToken cancellationToken = default)
    {
        if (_state.Location is null && context.RequestMessages.Any(x => x.Role == ChatRole.User))
        {
            Logger.LogInformation("Trying to extract user location for RestaurantsAgent");
            var result = await _chatClient.GetResponseAsync<UserLocation>(
                context.RequestMessages,
                new ChatOptions()
                {
                    Instructions = "Extract the user's location latitude and longitude from the message if present. If not present return null."
                },
                cancellationToken: cancellationToken);
            if (result.TryGetResult(out var location))
            {
                _state.Location = location;
                Logger.LogInformation("Extracted user location for RestaurantsAgent: {Location}", _state.Location);
            }
            else
            {
                Logger.LogInformation("No user location found in the message for RestaurantsAgent.");
            }
        }
    }

    public override async ValueTask<AIContext> InvokingAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        var aiContext = await base.InvokingAsync(context, cancellationToken);

        if (_state.Location is null)
        {
            Logger.LogInformation("Adding user location request to AI context");
            aiContext.Messages!.Add(new ChatMessage(
                ChatRole.User,
                $"Ask the user for their location coordinates (latitude and longitude). Decline to answer any questions until they provide it."));
        }

        return aiContext;
    }

    public override JsonElement Serialize(JsonSerializerOptions? jsonSerializerOptions = null)
    {
        Logger.LogDebug("Serializing BuildRestaurants domain agent AI context for AgentKey: {AgentKey}", AgentKey);
        return JsonSerializer.SerializeToElement(_state, jsonSerializerOptions);
    }

    public class InternalState
    {
        public UserLocation? Location { get; set; }
    }

    public record UserLocation(double Latitude, double Longitude);
}
