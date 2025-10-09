using System.Threading.Tasks;

namespace ChatBro.AiService.Services;

public interface IContextProvider
{
    Task<string> GetSystemContextAsync();
}

public class ContextProvider : IContextProvider
{
    public async Task<string> GetSystemContextAsync()
    {
        var filePath = Path.Combine(AppContext.BaseDirectory, "contexts", "shared.md");
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Context file not found: {filePath}");
        }
        var context = await File.ReadAllTextAsync(filePath);
        return context;
    }
}
