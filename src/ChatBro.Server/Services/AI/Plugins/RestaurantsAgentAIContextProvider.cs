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
                    Logger.LogDebug("Restored state for {AgentKey}: {State}", AgentKey, state);
                    return state;
                }
                Logger.LogDebug("No state found in serialized data for {AgentKey}. Initialize with empty state", AgentKey);
            }
            catch (JsonException ex)
            {
                Logger.LogError(ex,
                    "Failed to deserialize RestaurantsAgentAIContextProvider state for {AgentKey}. Initialize with empty state", AgentKey);
            }
        }

        return new InternalState();
    }

    public override async ValueTask InvokedAsync(InvokedContext context, CancellationToken cancellationToken = default)
    {
        if (_state.Location is null && context.RequestMessages.Any(x => x.Role == ChatRole.User))
        {
            var location = await ExtractLocationFromMessages(context.RequestMessages, cancellationToken);
            if (location is not null)
            {
                _state = new InternalState { Location = location };
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

    private async Task<UserLocation?> ExtractLocationFromMessages(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken = default)
    {
        Logger.LogInformation("Trying to extract user location for RestaurantsAgent");
        var result = await _chatClient.GetResponseAsync<UserLocation?>(
            messages,
            new ChatOptions()
            {
                Instructions = 
                    """
                    Analyze the conversation history. Extract ONLY explicitly stated latitude and longitude coordinates 
                    (as decimal numbers). If the user has NOT provided specific numeric coordinates, you MUST return null. 
                    Do NOT infer, guess, or return default values like 0,0.
                    """
            },
            cancellationToken: cancellationToken);

        if (!result.TryGetResult(out var location) || location is null)
        {
            Logger.LogInformation("No user location found in the message for RestaurantsAgent.");
            return null;
        }

        if (location.IsZeroCoordinate())
        {
            Logger.LogWarning("Extracted location is 0,0 (likely invalid), ignoring");
            return null;
        }
        else
        {
            Logger.LogInformation("Extracted user location for RestaurantsAgent: {Location}", location);
            return location;
        }
    }

    private static string FormatCoord(double value) => value.ToString("F7", System.Globalization.CultureInfo.InvariantCulture);

    public class InternalState
    {
        public UserLocation? Location { get; init; }

        public override string ToString() =>
            Location is null
                ? "No location"
                : $"Location: {Location}";
    }

    public record UserLocation
    {
        public double Latitude { get; init; }
        public double Longitude { get; init; }
        
        public UserLocation(double latitude, double longitude)
        {
            if (latitude is < -90 or > 90)
            {
                throw new ArgumentOutOfRangeException(nameof(latitude), latitude, "Latitude must be between -90 and 90 degrees.");
            }
            if (longitude is < -180 or > 180)
            {
                throw new ArgumentOutOfRangeException(nameof(longitude), longitude, "Longitude must be between -180 and 180 degrees.");
            }
            Latitude = latitude;
            Longitude = longitude;
        }

        public bool IsZeroCoordinate() =>
            Math.Abs(Latitude) < 1e-9 && Math.Abs(Longitude) < 1e-9;

        public override string ToString() => $"{FormatCoord(Latitude)}, {FormatCoord(Longitude)}";
    }
}
