using Microsoft.SemanticKernel;

namespace ChatBro.AiService.Plugins;

public class LoggingFilter(ILogger logger) : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        logger.LogInformation("Invoking plugin function - {PluginName}.{FunctionName}", context.Function.PluginName, context.Function.Name);
        await next(context);
    }
}
