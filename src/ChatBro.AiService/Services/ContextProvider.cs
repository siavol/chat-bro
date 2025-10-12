using Microsoft.Extensions.Options;
using ChatBro.AiService.Options;

namespace ChatBro.AiService.Services;

public interface IContextProvider
{
    Task<string> GetSystemContextAsync();
}

public class ContextProvider : IContextProvider
{
    private readonly ChatSettings _settings;

    public ContextProvider(IOptions<ChatSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task<string> GetSystemContextAsync()
    {
        var filePath = _settings.Context.Shared;
        if (!Path.IsPathRooted(filePath))
        {
            filePath = Path.Combine(AppContext.BaseDirectory, filePath);
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Context file not found: {filePath}");
        }

        var context = await File.ReadAllTextAsync(filePath);
        return context;
    }
}
