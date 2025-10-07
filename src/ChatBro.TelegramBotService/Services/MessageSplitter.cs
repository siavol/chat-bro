namespace ChatBro.TelegramBotService.Services;

public class MessageSplitter
{
    private const int TelegramMessageLimit = 4096;
    private const int MessageLengthLimit = 4000;
    private const int MessageLengthPreferred = 3800;
    private static readonly char[] SentenceBoundaries = ['.', '!', '?', ';', ':'];
    
    /// <summary>
    /// Splits a long text into multiple Telegram messages, 
    /// preferring natural breakpoints (paragraphs, sentences, words)
    /// while respecting the Telegram message character limit.
    /// Telegram message limit is 4096 characters, but we use lower value to leave some margin.
    /// </summary>
    /// <param name="text">Full text message</param>
    /// <param name="limit">Maximum Telegram message length</param>
    /// <param name="preferred">Preferred split length before searching for natural breakpoint</param>
    /// <returns>IEnumerable of message parts</returns>
    // ReSharper disable once MemberCanBeMadeStatic.Global
    public IEnumerable<string> SplitSmart(string text, int limit = MessageLengthLimit, int preferred = MessageLengthPreferred)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        if (limit > TelegramMessageLimit) throw new ArgumentOutOfRangeException(nameof(limit), $"Limit cannot exceed {TelegramMessageLimit} characters.");

        while (text.Length > limit)
        {
            var splitAt = FindBestSplitPosition(text, limit, preferred);
            yield return text[..splitAt].Trim();
            text = text[splitAt..].TrimStart();
        }

        if (!string.IsNullOrWhiteSpace(text))
        {
            yield return text;
        }
    }

    private static int FindBestSplitPosition(string text, int limit, int preferred)
    {
        var length = text.Length;

        // Start searching near the preferred zone
        var start = Math.Min(preferred, length - 1);
        var end = Math.Min(limit, length - 1);

        // Ensure start < end to avoid negative count in LastIndexOf
        if (start >= end)
        {
            return limit; // Fallback: hard split at limit
        }
        // Prefer double newlines (paragraphs)
        var splitAt = text.LastIndexOf("\n\n", start, end - start, StringComparison.Ordinal);
        if (splitAt != -1)
            return splitAt + 2;

        // Then single newline (line break)
        splitAt = text.LastIndexOf('\n', start, end - start);
        if (splitAt != -1)
            return splitAt + 1;

        // Then punctuation (sentence boundary)
        foreach (var ch in SentenceBoundaries)
        {
            splitAt = text.LastIndexOf(ch, start, end - start);
            if (splitAt != -1)
                return splitAt + 1;
        }

        // Then space (word boundary)
        splitAt = text.LastIndexOf(' ', start, end - start);
        return splitAt != -1 
            ? splitAt :
            limit; // Fallback: hard split at limit
    }
}