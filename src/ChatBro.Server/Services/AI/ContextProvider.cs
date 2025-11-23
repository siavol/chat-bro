using ChatBro.Server.Options;
using Microsoft.Extensions.Options;

namespace ChatBro.Server.Services.AI;

public interface IContextProvider
{
    Task<string> GetSystemContextAsync(string relativePath);
}

public class ContextProvider : IContextProvider
{
    private readonly ChatSettings _settings;

    public ContextProvider(IOptions<ChatSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task<string> GetSystemContextAsync(string relativePath)
    {
        var filePath = string.IsNullOrWhiteSpace(relativePath)
            ? _settings.Context.Shared
            : relativePath;
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

