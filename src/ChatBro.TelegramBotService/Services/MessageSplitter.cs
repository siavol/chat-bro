namespace ChatBro.TelegramBotService.Services;

public class MessageSplitter
{
    private const int MessageLengthLimit = 4000;
    private const int MessageLengthPreferred = 3800;
    private static readonly char[] SentenceBoundaries = ['.', '!', '?', ';', ':'];
    
    /// <summary>
    /// Splits a long text into multiple Telegram messages, 
    /// preferring natural breakpoints (paragraphs, sentences, words)
    /// while respecting the Telegram 4096 character limit.
    /// </summary>
    /// <param name="text">Full text message</param>
    /// <param name="limit">Maximum Telegram message length</param>
    /// <param name="preferred">Preferred split length before searching for natural breakpoint</param>
    // ReSharper disable once MemberCanBeMadeStatic.Global
    public List<string> SplitSmart(string text, int limit = MessageLengthLimit, int preferred = MessageLengthPreferred)
    {
        var parts = new List<string>();

        if (string.IsNullOrWhiteSpace(text))
        {
            return parts;
        }

        while (text.Length > limit)
        {
            var splitAt = FindBestSplitPosition(text, limit, preferred);
            parts.Add(text[..splitAt].Trim());
            text = text[splitAt..].TrimStart();
        }

        if (!string.IsNullOrWhiteSpace(text))
        {
            parts.Add(text);
        }

        return parts;
    }

    private static int FindBestSplitPosition(string text, int limit, int preferred)
    {
        var length = text.Length;

        // Start searching near the preferred zone
        var start = Math.Min(preferred, length - 1);
        var end = Math.Min(limit, length - 1);

        // Prefer double newlines (paragraphs)
        var splitAt = text.LastIndexOf("\n\n", end, end - start, StringComparison.Ordinal);
        if (splitAt != -1)
            return splitAt + 2;

        // Then single newline (line break)
        splitAt = text.LastIndexOf('\n', end, end - start);
        if (splitAt != -1)
            return splitAt + 1;

        // Then punctuation (sentence boundary)
        foreach (var ch in SentenceBoundaries)
        {
            splitAt = text.LastIndexOf(ch, end, end - start);
            if (splitAt != -1)
                return splitAt + 1;
        }

        // Then space (word boundary)
        splitAt = text.LastIndexOf(' ', end, end - start);
        return splitAt != -1 
            ? splitAt :
            limit; // Fallback: hard split at limit
    }
}