using System.ComponentModel.DataAnnotations;

namespace ChatBro.AiService.Options;

public class PaperlessMcpOptions
{
    /// <summary>
    /// Connection string for the Paperless MCP server.
    /// Expected format: http://host:port/mcp
    /// </summary>
    [Required(AllowEmptyStrings = false)]
    public string ConnectionString { get; set; } = string.Empty;
}
