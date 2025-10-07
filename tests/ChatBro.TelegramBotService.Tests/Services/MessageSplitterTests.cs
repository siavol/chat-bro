using ChatBro.TelegramBotService.Services;

namespace ChatBro.TelegramBotService.Tests.Services;

public class MessageSplitterTests
{
    private readonly MessageSplitter _splitter = new();

    [Fact]
    public void SplitSmart_EmptyString_ReturnsEmptyList()
    {
        var result = _splitter.SplitSmart("");
        Assert.Empty(result);
    }

    [Fact]
    public void SplitSmart_WhitespaceString_ReturnsEmptyList()
    {
        var result = _splitter.SplitSmart("   \n\t  ");
        Assert.Empty(result);
    }

    [Fact]
    public void SplitSmart_ShortMessage_ReturnsSinglePart()
    {
        const string text = "Hello, world!";
        var result = _splitter.SplitSmart(text);
        Assert.Single(result);
        Assert.Equal(text, result[0]);
    }

    [Fact]
    public void SplitSmart_MessageJustUnderLimit_ReturnsSinglePart()
    {
        var text = new string('a', 3999);
        var result = _splitter.SplitSmart(text);
        Assert.Single(result);
        Assert.Equal(text, result[0]);
    }

    [Fact]
    public void SplitSmart_MessageJustOverLimit_SplitsAtLimit()
    {
        var text = new string('a', 4001);
        var result = _splitter.SplitSmart(text, 4000, 3800);
        Assert.Equal(2, result.Count);
        Assert.Equal(new string('a', 4000), result[0]);
        Assert.Equal("a", result[1]);
    }

    [Fact]
    public void SplitSmart_SplitsAtParagraphBoundary()
    {
        var text = new string('a', 3800) + "\n\n" + new string('b', 1000);
        var result = _splitter.SplitSmart(text, 4000, 3800);
        Assert.Equal(2, result.Count);
        Assert.StartsWith(new string('b', 10), result[1].Substring(0, 10));
    }

    [Fact]
    public void SplitSmart_SplitsAtLineBoundary()
    {
        var text = new string('a', 3800) + "\n" + new string('b', 1000);
        var result = _splitter.SplitSmart(text, 4000, 3800);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void SplitSmart_SplitsAtSentenceBoundary()
    {
        var text = new string('a', 3800) + ". " + new string('b', 1000);
        var result = _splitter.SplitSmart(text, 4000, 3800);
        Assert.Equal(2, result.Count);
        Assert.EndsWith(".", result[0]);
    }

    [Fact]
    public void SplitSmart_SplitsAtWordBoundary()
    {
        var text = new string('a', 3800) + " " + new string('b', 1000);
        var result = _splitter.SplitSmart(text, 4000, 3800);
        Assert.Equal(2, result.Count);
        Assert.EndsWith("a", result[0]);
    }

    [Fact]
    public void SplitSmart_NoNaturalBoundary_HardSplit()
    {
        var text = new string('x', 4100);
        var result = _splitter.SplitSmart(text, 4000, 3800);
        Assert.Equal(2, result.Count);
        Assert.Equal(new string('x', 4000), result[0]);
        Assert.Equal(new string('x', 100), result[1]);
    }
}