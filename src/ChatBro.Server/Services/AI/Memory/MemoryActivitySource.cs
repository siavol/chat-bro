using System.Diagnostics;

namespace ChatBro.Server.Services.AI.Memory;

/// <summary>
/// Shared ActivitySource for observational memory operations.
/// Matched by the existing "ChatBro.*" wildcard in ServiceDefaults.
/// </summary>
public static class MemoryActivitySource
{
    public static readonly ActivitySource Source = new("ChatBro.ObservationalMemory");

    public static class SpanNames
    {
        public const string Load = "Memory.Load";
        public const string Save = "Memory.Save";
        public const string Delete = "Memory.Delete";
        public const string Observe = "Memory.Observe";
        public const string Reflect = "Memory.Reflect";
    }

    public static class TagKeys
    {
        public const string UserId = "memory.user_id";
        public const string ObservationsCount = "memory.observations.count";
        public const string RawMessagesCount = "memory.raw_messages.count";
    }
}
