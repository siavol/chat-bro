using ChatBro.AiService.Options;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Client;

namespace ChatBro.AiService.Services;

/// <summary>
/// Client for connecting to and retrieving tools from Paperless MCP server.
/// </summary>
public sealed class PaperlessMcpClient : IAsyncDisposable
{
    private readonly ILogger<PaperlessMcpClient> _logger;
    private readonly Task<McpClient> _mcpClientTask;
    private List<AITool>? _cachedTools;

    public PaperlessMcpClient(
        IOptions<PaperlessMcpOptions> options,
        ILogger<PaperlessMcpClient> logger)
    {
        _logger = logger;
        _mcpClientTask = InitializeClientAsync(options.Value.ConnectionString);
    }

    private async Task<McpClient> InitializeClientAsync(string paperlessMcpUrl)
    {
        if (string.IsNullOrEmpty(paperlessMcpUrl))
        {
            _logger.LogError("Paperless MCP connection string not found. MCP tools will not be available.");
            throw new InvalidOperationException("Paperless MCP connection string is not configured.");
        }

        _logger.LogDebug("Connecting to Paperless MCP server at {Url}", paperlessMcpUrl);
        var mcpClient = await McpClient.CreateAsync(new HttpClientTransport(new()
        {
            Endpoint = new Uri(paperlessMcpUrl)
        }));
        _logger.LogInformation("Successfully connected to Paperless MCP server");
        return mcpClient;
    }

    /// <summary>
    /// Gets the list of AI tools available from the Paperless MCP server.
    /// Tools are cached after the first retrieval.
    /// </summary>
    /// <returns>List of AI tools, or empty list if not connected.</returns>
    public async Task<IReadOnlyList<AITool>> GetToolsAsync()
    {
        if (_cachedTools != null)
        {
            return _cachedTools;
        }

        try
        {
            var mcpClient = await _mcpClientTask;        

            var mcpTools = await mcpClient.ListToolsAsync();
            _cachedTools = [.. mcpTools];
            _logger.LogInformation("Retrieved and cached {Count} tools from Paperless MCP server", _cachedTools.Count);
            return _cachedTools;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve tools from Paperless MCP server. Returning empty list.");
            _cachedTools = [];
            return _cachedTools;
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            var mcpClient = await _mcpClientTask;

            if (mcpClient != null)
            {
                try
                {
                    await mcpClient.DisposeAsync();
                    _logger.LogInformation("Paperless MCP client disposed");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing Paperless MCP client");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error awaiting MCP client task during disposal");
        }
    }
}
