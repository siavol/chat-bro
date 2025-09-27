#pragma warning disable ASPIREINTERACTION001

using Microsoft.Extensions.Configuration;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);
builder.AddDockerComposeEnvironment("docker-compose");

var telegramToken = CreateTelegramTokenParameter();
var telegramBot = builder.AddProject<ChatBro_TelegramBotService>("telegram-bot")
    .WithEnvironment("Telegram__Token", telegramToken);

var ollama = builder.AddOllama("ollama")
    .WithDataVolume()
    // .WithContainerRuntimeArgs("--gpus=all")
    .WithOpenWebUI();
var aiModel = ollama.AddModel(name: "ai-model", modelName: "llama3.1:8b");
builder.AddProject<ChatBro_AiService>("ai-service")
    .WithReference(aiModel).WaitFor(aiModel);

builder.Build().Run();

return;

IResourceBuilder<ParameterResource> CreateTelegramTokenParameter()
{
    const string parameterName = "telegram-token";
    var configValue = builder.Configuration.GetValue<string>(parameterName);
    if (configValue is not null)
    {
        return builder.AddParameter(parameterName, configValue);
    }

    return builder.AddParameter(parameterName)
        .WithDescription("Telegram bot token.")
        .WithCustomInput(p => new InteractionInput
        {
            Name = parameterName,
            InputType = InputType.SecretText,
            Label = p.Name,
            Placeholder = "Enter token secret:secret"
        });
}