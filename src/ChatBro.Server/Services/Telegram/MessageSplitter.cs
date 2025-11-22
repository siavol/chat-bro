namespace ChatBro.Server.Services.Telegram;

public class MessageSplitter
{
    private const int TelegramMessageLimit = 4096;
    private const int MessageLengthLimit = 4000;
    private const int MessageLengthPreferred = 3800;
    private static readonly char[] SentenceBoundaries = ['.', '!', '?', ';', ':'];

    /// <summary>
    /// Splits a long text into multiple Telegram messages while preferring natural breakpoints.
    /// </summary>
    public IEnumerable<string> SplitSmart(string text, int limit = MessageLengthLimit, int preferred = MessageLengthPreferred)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        if (limit > TelegramMessageLimit)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), $"Limit cannot exceed {TelegramMessageLimit} characters.");
        }

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
        var start = Math.Min(preferred, length - 1);
        var end = Math.Min(limit, length - 1);

        if (start >= end)
        {
            return limit;
        }

        var splitAt = text.LastIndexOf("\n\n", start, end - start, StringComparison.Ordinal);
        if (splitAt != -1)
        {
            return splitAt + 2;
        }

        splitAt = text.LastIndexOf('\n', start, end - start);
        if (splitAt != -1)
        {
            return splitAt + 1;
        }

        foreach (var ch in SentenceBoundaries)
        {
            splitAt = text.LastIndexOf(ch, start, end - start);
            if (splitAt != -1)
            {
                return splitAt + 1;
            }
        }

        splitAt = text.LastIndexOf(' ', start, end - start);
        return splitAt != -1 ? splitAt : limit;
    }
}

