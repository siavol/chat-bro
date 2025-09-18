namespace ChatBro.E2E.Tests.StepDefinitions;

public class HttpContext
{
    private HttpClient? _client;
    
    public HttpClient Client => _client 
        ?? throw new InvalidOperationException($"Use {nameof(InitializeHttpClient)} to initialize the client.");

    public void InitializeHttpClient(HttpClient httpClient)
    {
        _client = httpClient;
    }
}