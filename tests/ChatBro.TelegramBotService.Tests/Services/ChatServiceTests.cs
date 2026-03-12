using ChatBro.Server.Options;
using ChatBro.Server.Services.AI;
using ChatBro.Server.Services.AI.Memory;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ChatBro.TelegramBotService.Tests.Services;

public class ChatServiceTests
{
    private static ChatService CreateService(FakeSessionStore? threadStore = null, FakeDomainToolingBuilder? domainBuilder = null, FakeMemoryStore? memoryStore = null)
    {
        return new ChatService(
            new FakeAgentProvider(),
            threadStore ?? new FakeSessionStore(false),
            domainBuilder ?? new FakeDomainToolingBuilder(),
            memoryStore ?? new FakeMemoryStore(),
            new ObservationalMemoryContext(),
            new FakeObserverService(),
            new FakeReflectorService(),
            Options.Create(new ObservationalMemorySettings()),
            NullLogger<ChatService>.Instance);
    }

    [Fact]
    public async Task ResetChatAsync_WhenStoreDeletes_ReturnsTrue()
    {
        var threadStore = new FakeSessionStore(true);
        var service = CreateService(threadStore: threadStore);

        var result = await service.ResetChatAsync("user-1");

        Assert.True(result);
        Assert.Equal("user-1", threadStore.LastDeletedUserId);
    }

    [Fact]
    public async Task ResetChatAsync_WhenStoreHasNoHistory_ReturnsFalse()
    {
        var threadStore = new FakeSessionStore(false);
        var service = CreateService(threadStore: threadStore);

        var result = await service.ResetChatAsync("user-2");

        Assert.False(result);
        Assert.Equal("user-2", threadStore.LastDeletedUserId);
    }

    [Fact]
    public async Task ResetChatAsync_DoesNotDeleteMemory()
    {
        var memoryStore = new FakeMemoryStore();
        var service = CreateService(memoryStore: memoryStore);

        await service.ResetChatAsync("user-3");

        Assert.Null(memoryStore.LastDeletedUserId);
    }

    [Fact]
    public async Task HardResetChatAsync_DeletesMemory()
    {
        var threadStore = new FakeSessionStore(true);
        var memoryStore = new FakeMemoryStore();
        var service = CreateService(threadStore: threadStore, memoryStore: memoryStore);

        var result = await service.HardResetChatAsync("user-4");

        Assert.True(result);
        Assert.Equal("user-4", threadStore.LastDeletedUserId);
        Assert.Equal("user-4", memoryStore.LastDeletedUserId);
    }

    [Fact]
    public async Task HardResetChatAsync_DeletesMemoryEvenWhenNoThreads()
    {
        var threadStore = new FakeSessionStore(false);
        var memoryStore = new FakeMemoryStore();
        var service = CreateService(threadStore: threadStore, memoryStore: memoryStore);

        var result = await service.HardResetChatAsync("user-5");

        Assert.False(result);
        Assert.Equal("user-5", memoryStore.LastDeletedUserId);
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

    private sealed class FakeSessionStore : IAgentSessionStore
    {
        private readonly bool _deleteResult;

        public FakeSessionStore(bool deleteResult)
        {
            _deleteResult = deleteResult;
        }

        public string? LastDeletedUserId { get; private set; }

        public Task<AgentSession> GetThreadAsync(string userId, AIAgent agent)
        {
            throw new NotSupportedException();
        }

        public Task SaveThreadAsync(string userId, AIAgent agent, AgentSession thread)
        {
            throw new NotSupportedException();
        }

        public Task<bool> DeleteThreadAsync(string userId)
        {
            LastDeletedUserId = userId;
            return Task.FromResult(_deleteResult);
        }
    }

    private sealed class FakeMemoryStore : IObservationalMemoryStore
    {
        public string? LastDeletedUserId { get; private set; }

        public Task<UserMemory> LoadAsync(string userId, CancellationToken cancellationToken = default)
            => Task.FromResult(new UserMemory());

        public Task SaveAsync(string userId, UserMemory memory, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DeleteAsync(string userId, CancellationToken cancellationToken = default)
        {
            LastDeletedUserId = userId;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeObserverService : IObserverService
    {
        public Task<UserMemory> ObserveAsync(UserMemory memory, CancellationToken cancellationToken = default)
            => Task.FromResult(memory);
    }

    private sealed class FakeReflectorService : IReflectorService
    {
        public Task<UserMemory> ReflectAsync(UserMemory memory, CancellationToken cancellationToken = default)
            => Task.FromResult(memory);
    }
}
