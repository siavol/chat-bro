namespace ChatBro.Server.Services.AI;

public interface IDomainToolingBuilder
{
    Task<DomainTooling> CreateAsync(string userId, CancellationToken cancellationToken = default);

    Task<bool> ResetAsync(string userId, CancellationToken cancellationToken = default);
}
