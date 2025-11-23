using System.Collections.Generic;
using System.Threading;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace ChatBro.Server.Services;

public sealed class DomainToolingBuilder(
    IAIAgentProvider agentProvider,
    IAgentThreadStore threadStore,
    ILogger<DomainToolingBuilder> logger) : IDomainToolingBuilder
{
    public async Task<DomainTooling> CreateAsync(string userId, CancellationToken cancellationToken = default)
    {
        var domainAgents = await agentProvider.GetDomainAgentsAsync(cancellationToken);
        var tools = new List<AITool>(domainAgents.Count);
        var threadHandles = new List<DomainThreadHandle>(domainAgents.Count);

        foreach (var domain in domainAgents)
        {
            var domainThreadKey = BuildThreadKey(userId, domain.Key);
            var thread = await threadStore.GetThreadAsync(domainThreadKey, domain.Agent);
            logger.LogDebug("Prepared thread {ThreadKey} for domain {DomainKey}", domainThreadKey, domain.Key);

            var tool = domain.Agent.AsAIFunction(
                new AIFunctionFactoryOptions
                {
                    Name = domain.ToolName,
                    Description = domain.Description
                },
                thread);

            tools.Add(tool);
            threadHandles.Add(new DomainThreadHandle(domainThreadKey, thread));
        }

        var runOptions = new ChatClientAgentRunOptions(new ChatOptions
        {
            Tools = tools
        });

        return new DomainTooling(runOptions, threadHandles);
    }

    public async Task<bool> ResetAsync(string userId, CancellationToken cancellationToken = default)
    {
        var domainAgents = await agentProvider.GetDomainAgentsAsync(cancellationToken);
        var deletedAny = false;

        foreach (var domain in domainAgents)
        {
            var domainThreadKey = BuildThreadKey(userId, domain.Key);
            var deleted = await threadStore.DeleteThreadAsync(domainThreadKey);
            if (deleted)
            {
                deletedAny = true;
                logger.LogInformation("Reset thread for domain {DomainKey} and user key {ThreadKey}", domain.Key, domainThreadKey);
            }
        }

        return deletedAny;
    }

    public static string BuildThreadKey(string userId, string domainKey)
        => $"{userId}::{domainKey}";
}

public sealed record DomainThreadHandle(string ThreadKey, AgentThread Thread);

public sealed record DomainTooling(
    ChatClientAgentRunOptions RunOptions,
    IReadOnlyList<DomainThreadHandle> DomainThreads);
