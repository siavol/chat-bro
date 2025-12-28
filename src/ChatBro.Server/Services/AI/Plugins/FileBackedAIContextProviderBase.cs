using Microsoft.Agents.AI;

namespace ChatBro.Server.Services.AI.Plugins;

public abstract class FileBackedAIContextProviderBase : AIContextProvider
{
    protected const string ContextsFolder = "contexts";
    protected const string DomainsFolder = "domains";

    protected static async Task<string> GetSystemContextAsync(string contextPath, CancellationToken cancellationToken = default) 
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contextPath);
        var filePath = Path.IsPathRooted(contextPath) 
            ? contextPath 
            : Path.Combine(AppContext.BaseDirectory, contextPath);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Context file not found: {filePath}");
        }

        var context = await File.ReadAllTextAsync(filePath, cancellationToken);
        return context;
    }
}
