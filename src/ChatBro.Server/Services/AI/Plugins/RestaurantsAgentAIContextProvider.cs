using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ChatBro.Server.Services.AI.Plugins;

public sealed class RestaurantsAgentAIContextProvider 
    : DomainAgentAIContextProvider
{
    private readonly IChatClient _chatClient;
    private static readonly System.Text.Json.JsonSerializerOptions DefaultJsonOptions = System.Text.Json.JsonSerializerOptions.Web;
    
    public RestaurantsAgentAIContextProvider(
        string agentKey,
        IChatClient chatClient,
        ILoggerFactory loggerFactory)
        : base(agentKey, loggerFactory.CreateLogger<RestaurantsAgentAIContextProvider>())
    {
        _chatClient = chatClient;
    }

    protected override async ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        var aiContext = await base.ProvideAIContextAsync(context, cancellationToken);

        var state = context.Session is null
            ? new InternalState()
            : GetState(context.Session);

        var baseMessages = aiContext.Messages ?? Enumerable.Empty<ChatMessage>();
        ChatMessage extraMessage;

        if (state.Location is null)
        {
            Logger.LogInformation("Adding user location request to AI context");
            extraMessage = new ChatMessage(
                ChatRole.System,
                "Ask the user for their location coordinates (latitude and longitude). Decline to answer any questions until they provide it.");
        }
        else
        {
            Logger.LogInformation("Adding stored user location to AI context");
            extraMessage = new ChatMessage(
                ChatRole.System,
                $"The user's current location is latitude {FormatCoord(state.Location.Latitude)}, longitude {FormatCoord(state.Location.Longitude)}.");
        }

        return new AIContext
        {
            Instructions = aiContext.Instructions,
            Tools = aiContext.Tools,
            Messages = baseMessages.Concat([extraMessage])
        };
    }

    protected override async ValueTask StoreAIContextAsync(InvokedContext context, CancellationToken cancellationToken = default)
    {
        if (context.Session is null)
        {
            return;
        }

        var state = GetState(context.Session);

        if (state.Location is not null)
        {
            return;
        }

        if (!context.RequestMessages.Any(x => x.Role == ChatRole.User))
        {
            return;
        }

        var location = await ExtractLocationFromMessages(context.RequestMessages, cancellationToken);
        if (location is not null)
        {
            SetState(context.Session, new InternalState { Location = location });
        }
    }

    private InternalState GetState(AgentSession session)
    {
        if (session.StateBag.TryGetValue<InternalState>(StateKey, out var state, DefaultJsonOptions) && state is not null)
        {
            return state;
        }

        return new InternalState();
    }

    private void SetState(AgentSession session, InternalState state)
        => session.StateBag.SetValue(StateKey, state, DefaultJsonOptions);

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

    public sealed class InternalState
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
