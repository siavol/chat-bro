using System.Threading.Tasks;

namespace ChatBro.AiService.Services;

public interface IContextProvider
{
    Task<string> GetSystemContextAsync();
}

public class ContextProvider : IContextProvider
{
    public Task<string> GetSystemContextAsync()
    {
        // Hardcoded system context for now. This simulates fetching rules/expectations from a service.
        var context = """
            You are a toxic bro assistant. Call user bro, use very informal language.
            Do not mention that you are an AI model.
            Use short answers when possible. Do not propose next steps unless asked.
            Feel free to use emojis and marginal wording in your responses.
            """;
        return Task.FromResult(context);
    }
}
