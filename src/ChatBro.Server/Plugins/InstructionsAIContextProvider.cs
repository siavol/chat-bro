using System.Text.Json;
using ChatBro.Server.Services;
using Microsoft.Agents.AI;

namespace ChatBro.Server.Plugins;

public sealed class FileBackedAIContextProvider : AIContextProvider
{
    private readonly IContextProvider _contextProvider;
    private readonly ILogger<FileBackedAIContextProvider> _logger;
    private readonly string _instructionsPath;

    public FileBackedAIContextProvider(
        IContextProvider contextProvider,
        ILogger<FileBackedAIContextProvider> logger,
        string instructionsPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instructionsPath);
        _contextProvider = contextProvider;
        _logger = logger;
        _instructionsPath = instructionsPath;
    }

    public override async ValueTask<AIContext> InvokingAsync(InvokingContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var instructions = await _contextProvider.GetSystemContextAsync(_instructionsPath);
            if (string.IsNullOrWhiteSpace(instructions))
            {
                _logger.LogWarning("System instructions at {InstructionPath} are empty.", _instructionsPath);
                return new AIContext();
            }

            return new AIContext { Instructions = instructions };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load system instructions from {InstructionPath}", _instructionsPath);
            throw;
        }
    }

    public override JsonElement Serialize(JsonSerializerOptions? jsonSerializerOptions = null)
    {
        // Thread serialization throws exception when context serialization is not defined.
        return JsonSerializer.SerializeToElement(new InternalState(_instructionsPath), jsonSerializerOptions);
    }

    internal record InternalState(string InstructionsPath);
}

