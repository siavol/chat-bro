namespace ChatBro.TelegramBotService.Clients;

public class AiServiceClient(HttpClient http)
{
    public async Task<string> ChatAsync(string message, CancellationToken cancellationToken = default)
    {
        var request = new ChatRequest(message);

        using var response = await http.PostAsync("/chat", JsonContent.Create(request), cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<ChatResponse>(cancellationToken: cancellationToken);
        if (body is null)
        {
            throw new InvalidOperationException("Could not deserialize chat response");
        }

        return body.TextContent;
    }

    private record ChatRequest(string Message);
    private record ChatResponse(string TextContent);
}
