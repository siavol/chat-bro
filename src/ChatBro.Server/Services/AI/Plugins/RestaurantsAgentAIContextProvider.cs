using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ChatBro.Server.Services.AI.Plugins;

public sealed class RestaurantsAgentAIContextProvider 
    : DomainAgentAIContextProvider
{
    private readonly IChatClient _chatClient;
    private readonly InternalState _state;
    
    public RestaurantsAgentAIContextProvider(
        string agentKey,
        IChatClient chatClient,
        ILoggerFactory loggerFactory,
        JsonElement serializedState, 
        JsonSerializerOptions? jsonSerializerOptions = null)
        : base(agentKey, loggerFactory.CreateLogger<RestaurantsAgentAIContextProvider>())
    {
        _chatClient = chatClient;

        _state = RestoreState(serializedState, jsonSerializerOptions);
    }

    private InternalState RestoreState(JsonElement serializedState, JsonSerializerOptions? jsonSerializerOptions)
    {
        if (serializedState.ValueKind == JsonValueKind.Object)
        {
            try
            {
                var state = serializedState.Deserialize<InternalState>(jsonSerializerOptions);
                if (state != null)
                {
                    return state;
                }
                Logger.LogDebug("No state found in serialized data for AgentKey: {AgentKey}. Initialize with empty state", AgentKey);
            }
            catch (JsonException ex)
            {
                Logger.LogError(ex,
                    "Failed to deserialize RestaurantsAgentAIContextProvider state for AgentKey: {AgentKey}. Initialize with empty state", AgentKey);
            }
        }

        return new InternalState();
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
            aiContext.Messages!.Add(new ChatMessage(ChatRole.System,
                $"Ask the user for their location coordinates (latitude and longitude). Decline to answer any questions until they provide it."));
        }
        else
        {
            Logger.LogInformation("Adding stored user location to AI context");
            aiContext.Messages!.Add(new ChatMessage(ChatRole.System,
                $"The user's current location is latitude {FormatCoord(_state.Location.Latitude)}, longitude {FormatCoord(_state.Location.Longitude)}."));
        }

        return aiContext;
    }

    public override JsonElement Serialize(JsonSerializerOptions? jsonSerializerOptions = null)
    {
        Logger.LogDebug("Serializing Restaurants domain agent AI context for AgentKey: {AgentKey}", AgentKey);
        return JsonSerializer.SerializeToElement(_state, jsonSerializerOptions);
    }

    private static string FormatCoord(double value) => value.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);

    public class InternalState
    {
        public UserLocation? Location { get; internal set; }
    }

    public record UserLocation(double Latitude, double Longitude);
}
