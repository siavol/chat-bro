namespace ChatBro.Server.Services.AI.Memory;

/// <summary>
/// Carries per-request <see cref="UserMemory"/> from scoped <see cref="ChatService"/>
/// into singleton <see cref="MemoryAIContextProvider"/> via <see cref="AsyncLocal{T}"/>.
/// Registered as a singleton in DI to ensure a single shared instance.
/// </summary>
public sealed class ObservationalMemoryContext
{
    private readonly AsyncLocal<UserMemory?> _current = new();

    /// <summary>
    /// Gets or sets the current user's memory for this async flow.
    /// Set before agent run, cleared in finally block.
    /// </summary>
    public UserMemory? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }
}
