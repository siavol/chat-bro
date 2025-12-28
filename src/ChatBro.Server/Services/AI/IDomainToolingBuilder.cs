namespace ChatBro.Server.Services.AI;

/// <summary>  
/// Defines a builder for creating and managing domain-specific tooling used by AI services.  
/// Domain tooling typically encapsulates tools, configuration, and context that tailor  
/// AI behavior to a particular user or application domain.  
/// </summary> 
public interface IDomainToolingBuilder
{
    Task<DomainTooling> CreateAsync(string userId, CancellationToken cancellationToken = default);

    Task<bool> ResetAsync(string userId, CancellationToken cancellationToken = default);
}
