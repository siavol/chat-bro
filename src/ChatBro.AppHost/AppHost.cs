#pragma warning disable ASPIREINTERACTION001

using Microsoft.Extensions.Configuration;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);
builder.AddDockerComposeEnvironment("docker-compose");


var ollama = builder.AddOllama("ollama")
    .WithDataVolume();
    // .WithContainerRuntimeArgs("--gpus=all")
    // .WithOpenWebUI();
var aiModel = ollama.AddModel(name: "ai-model", modelName: "llama3.2:latest");
var aiService = builder.AddProject<ChatBro_AiService>("ai-service")
    .WithReference(aiModel).WaitFor(aiModel);


var telegramToken = CreateTelegramTokenParameter();
builder.AddProject<ChatBro_TelegramBotService>("telegram-bot")
    .WithEnvironment("Telegram__Token", telegramToken)
    .WithReference(aiService).WaitFor(aiService);


builder.AddProject<ChatBro_RestaurantsService>("restaurants");


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