namespace ChatBro.Server.Services.AI.Plugins;

public sealed class GenericDomainAgentAIContextProvider(
    string agentKey,
    ILoggerFactory loggerFactory) 
    : DomainAgentAIContextProvider(agentKey, loggerFactory.CreateLogger<GenericDomainAgentAIContextProvider>())
{
}
