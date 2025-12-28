using System.Diagnostics;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ChatBro.Server.Services.AI.Plugins;

public class FunctionMiddleware(ILogger<FunctionMiddleware> logger)
{
    public async ValueTask<object?> CustomFunctionCallingMiddleware(
        AIAgent agent,
        FunctionInvocationContext context,
        Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next,
        CancellationToken cancellationToken)
    {
        using var activity = Activity.Current?.Source.CreateActivity($"invoke_function {context.Function.Name}", ActivityKind.Internal)
            ?.SetTag("function.name", context.Function.Name)
            ?.Start();
        
        logger.LogInformation("Invoking plugin function - {FunctionName}", context.Function.Name);
        var result = await next(context, cancellationToken);
        logger.LogInformation("Function {FunctionName} call result: {Result}", context.Function.Name, result);
        
        activity?.SetStatus(ActivityStatusCode.Ok);
        return result;
    }
}

