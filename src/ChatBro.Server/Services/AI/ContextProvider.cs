namespace ChatBro.Server.Services.AI;

public interface IContextProvider
{
    Task<string> GetSystemContextAsync(string contextPath);
}

public class ContextProvider : IContextProvider
{
    // private readonly ChatSettings _settings;

    public ContextProvider()
    {
        //_settings = settings.Value;
    }

    public async Task<string> GetSystemContextAsync(string contextPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contextPath);
        string? filePath = Path.IsPathRooted(contextPath) 
            ? contextPath 
            : Path.Combine(AppContext.BaseDirectory, contextPath);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Context file not found: {filePath}");
        }

        var context = await File.ReadAllTextAsync(filePath);
        return context;
    }
}

