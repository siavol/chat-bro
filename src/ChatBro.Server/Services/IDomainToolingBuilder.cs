using System.Threading;

namespace ChatBro.Server.Services;

public interface IDomainToolingBuilder
{
    Task<DomainTooling> CreateAsync(string userId, CancellationToken cancellationToken = default);

    Task<bool> ResetAsync(string userId, CancellationToken cancellationToken = default);
}
