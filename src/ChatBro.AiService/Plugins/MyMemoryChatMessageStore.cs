using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ChatBro.AiService.Plugins;

public class MyMemoryChatMessageStore : ChatMessageStore
{
    private readonly Dictionary<string, List<ChatMessage>> _store = new();

    public string? ThreadKey { get; private set; }

    public MyMemoryChatMessageStore(
        JsonElement serializedStoreState,
        JsonSerializerOptions? jsonSerializerOptions = null)
    {
        if (serializedStoreState.ValueKind is JsonValueKind.String)
        {
            ThreadKey = serializedStoreState.Deserialize<string>();
        }
    }

    public override Task AddMessagesAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken = default)
    {
        this.ThreadKey ??= Guid.NewGuid().ToString("N");

        var storeMessages = _store.GetValueOrDefault(this.ThreadKey, new List<ChatMessage>());
        storeMessages.AddRange(messages);
        _store[ThreadKey] = storeMessages;
        
        return Task.CompletedTask;
    }

    public override Task<IEnumerable<ChatMessage>> GetMessagesAsync(CancellationToken cancellationToken = default)
    {
        if (this.ThreadKey is null || !_store.ContainsKey(this.ThreadKey))
        {
            return Task.FromResult(Enumerable.Empty<ChatMessage>());
        }

        var messages = _store[ThreadKey];
        return Task.FromResult(messages.AsEnumerable());
    }

    public override JsonElement Serialize(JsonSerializerOptions? jsonSerializerOptions = null)
    {
        // We have to serialize the thread id, so that on deserialization you can retrieve the messages using the same thread id.
        return JsonSerializer.SerializeToElement(ThreadKey);
    }
}