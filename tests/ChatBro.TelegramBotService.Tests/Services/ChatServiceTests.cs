using System.Collections.Generic;
using System.Threading;
using ChatBro.Server.Services;
using ChatBro.Server.Services.AI;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Logging.Abstractions;

namespace ChatBro.TelegramBotService.Tests.Services;

public class ChatServiceTests
{
    [Fact]
    public async Task ResetChatAsync_WhenStoreDeletes_ReturnsTrue()
    {
        var threadStore = new FakeThreadStore(true);
        var domainBuilder = new FakeDomainToolingBuilder();
        var service = new ChatService(new FakeAgentProvider(), threadStore, domainBuilder, NullLogger<ChatService>.Instance);

        var result = await service.ResetChatAsync("user-1");

        Assert.True(result);
        Assert.Equal("user-1", threadStore.LastDeletedUserId);
    }

    [Fact]
    public async Task ResetChatAsync_WhenStoreHasNoHistory_ReturnsFalse()
    {
        var threadStore = new FakeThreadStore(false);
        var domainBuilder = new FakeDomainToolingBuilder();
        var service = new ChatService(new FakeAgentProvider(), threadStore, domainBuilder, NullLogger<ChatService>.Instance);

        var result = await service.ResetChatAsync("user-2");

        Assert.False(result);
        Assert.Equal("user-2", threadStore.LastDeletedUserId);
    }

    private sealed class FakeAgentProvider : IAIAgentProvider
    {
        public Task<AIAgent> GetAgentAsync() => throw new NotSupportedException();

        public Task<IReadOnlyList<DomainAgentRegistration>> GetDomainAgentsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<DomainAgentRegistration>>(Array.Empty<DomainAgentRegistration>());
    }

    private sealed class FakeDomainToolingBuilder : IDomainToolingBuilder
    {
        public Task<DomainTooling> CreateAsync(string userId, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> ResetAsync(string userId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }
    }

    private sealed class FakeThreadStore : IAgentThreadStore
    {
        private readonly bool _deleteResult;

        public FakeThreadStore(bool deleteResult)
        {
            _deleteResult = deleteResult;
        }

        public string? LastDeletedUserId { get; private set; }

        public Task<AgentSession> GetThreadAsync(string userId, AIAgent agent)
        {
            throw new NotSupportedException();
        }

        public Task SaveThreadAsync(string userId, AgentSession thread)
        {
            throw new NotSupportedException();
        }

        public Task<bool> DeleteThreadAsync(string userId)
        {
            LastDeletedUserId = userId;
            return Task.FromResult(_deleteResult);
        }
    }
}
