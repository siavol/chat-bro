using ChatBro.Server.Services;
using Microsoft.Agents.AI;
using Microsoft.Extensions.Logging.Abstractions;

namespace ChatBro.TelegramBotService.Tests.Services;

public class ChatServiceTests
{
    [Fact]
    public async Task ResetChatAsync_WhenStoreDeletes_ReturnsTrue()
    {
        var threadStore = new FakeThreadStore(true);
        var service = new ChatService(new FakeAgentProvider(), threadStore, NullLogger<ChatService>.Instance);

        var result = await service.ResetChatAsync("user-1");

        Assert.True(result);
        Assert.Equal("user-1", threadStore.LastDeletedUserId);
    }

    [Fact]
    public async Task ResetChatAsync_WhenStoreHasNoHistory_ReturnsFalse()
    {
        var threadStore = new FakeThreadStore(false);
        var service = new ChatService(new FakeAgentProvider(), threadStore, NullLogger<ChatService>.Instance);

        var result = await service.ResetChatAsync("user-2");

        Assert.False(result);
        Assert.Equal("user-2", threadStore.LastDeletedUserId);
    }

    private sealed class FakeAgentProvider : IAIAgentProvider
    {
        public Task<AIAgent> GetAgentAsync()
        {
            throw new NotSupportedException();
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

        public Task<AgentThread> GetThreadAsync(string userId, AIAgent agent)
        {
            throw new NotSupportedException();
        }

        public Task SaveThreadAsync(string userId, AgentThread thread)
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
