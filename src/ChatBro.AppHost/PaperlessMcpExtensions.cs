using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// A resource that represents a Paperless MCP server.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="paperlessUrl">The Paperless-NGX server URL parameter.</param>
/// <param name="paperlessApiKey">The Paperless-NGX API key parameter.</param>
public class PaperlessMcpResource(string name, ParameterResource paperlessUrl, ParameterResource paperlessApiKey) 
    : ContainerResource(name), IResourceWithConnectionString
{
    internal const string PrimaryEndpointName = "mcp";
    
    private EndpointReference? _primaryEndpoint;
    
    /// <summary>
    /// Gets the primary endpoint for the Paperless MCP server.
    /// </summary>
    public EndpointReference PrimaryEndpoint => _primaryEndpoint ??= new(this, PrimaryEndpointName);
    
    /// <summary>
    /// Gets the Paperless-NGX server URL parameter.
    /// </summary>
    public ParameterResource PaperlessUrl { get; } = paperlessUrl;
    
    /// <summary>
    /// Gets the Paperless-NGX API key parameter.
    /// </summary>
    public ParameterResource PaperlessApiKey { get; } = paperlessApiKey;
    
    /// <summary>
    /// Gets the connection string expression for the Paperless MCP server.
    /// The connection string is the HTTP endpoint URL with /mcp path.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression
    {
        get
        {
            if (this.TryGetLastAnnotation<ConnectionStringRedirectAnnotation>(out var connectionStringAnnotation))
            {
                return connectionStringAnnotation.Resource.ConnectionStringExpression;
            }
            
            var builder = new ReferenceExpressionBuilder();
            builder.Append($"{PrimaryEndpoint.Property(EndpointProperty.Url)}");
            builder.AppendLiteral("/mcp");
            return builder.Build();
        }
    }
    
    /// <summary>
    /// Gets the connection string for the Paperless MCP server.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A connection string for the MCP server in the form "http://host:port/mcp".</returns>
    public ValueTask<string?> GetConnectionStringAsync(CancellationToken cancellationToken = default)
    {
        if (this.TryGetLastAnnotation<ConnectionStringRedirectAnnotation>(out var connectionStringAnnotation))
        {
            return connectionStringAnnotation.Resource.GetConnectionStringAsync(cancellationToken);
        }
        
        return ConnectionStringExpression.GetValueAsync(cancellationToken);
    }
}

public static class PaperlessMcpExtensions
{
    /// <summary>
    /// Adds a Paperless MCP server container to the application.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="name">The name of the resource.</param>
    /// <param name="paperlessUrl">The Paperless-NGX server URL.</param>
    /// <param name="paperlessApiKey">The Paperless-NGX API key.</param>
    /// <param name="tag">The image tag to use. Defaults to "latest".</param>
    /// <returns>A resource builder for the Paperless MCP server.</returns>
    public static IResourceBuilder<PaperlessMcpResource> AddPaperlessMcp(
        this IDistributedApplicationBuilder builder,
        string name,
        IResourceBuilder<ParameterResource> paperlessUrl,
        IResourceBuilder<ParameterResource> paperlessApiKey,
        string tag = "latest")
    {
        var resource = new PaperlessMcpResource(name, paperlessUrl.Resource, paperlessApiKey.Resource);
        
        return builder.AddResource(resource)
            .WithImage("ghcr.io/baruchiro/paperless-mcp")
            .WithImageTag(tag)
            .WithHttpEndpoint(targetPort: 3000, name: PaperlessMcpResource.PrimaryEndpointName)
            .WithEnvironment("PAPERLESS_URL", paperlessUrl)
            .WithEnvironment("PAPERLESS_API_KEY", paperlessApiKey);
    }
}
