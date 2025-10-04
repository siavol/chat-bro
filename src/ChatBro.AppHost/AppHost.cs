#pragma warning disable ASPIREINTERACTION001

using Microsoft.Extensions.Configuration;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);
builder.AddDockerComposeEnvironment("docker-compose");


var restaurants = builder.AddProject<ChatBro_RestaurantsService>("restaurants");

var openAiApiKey = CreateOpenAiApiKeyParameter();
var openAiModel = builder.AddParameter("openai-model", value: "o4-mini", publishValueAsDefault: true);
var aiService = builder.AddProject<ChatBro_AiService>("ai-service")
    .WithEnvironment("OpenAI__ApiKey", openAiApiKey)
    .WithEnvironment("OpenAI__Model", openAiModel)
    .WithReference(restaurants).WaitFor(restaurants);


var telegramToken = CreateTelegramTokenParameter();
builder.AddProject<ChatBro_TelegramBotService>("telegram-bot")
    .WithEnvironment("Telegram__Token", telegramToken)
    .WithReference(aiService).WaitFor(aiService);


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

    return builder.AddParameter(parameterName, secret: true)
        .WithDescription("Telegram bot token.")
        .WithCustomInput(p => new InteractionInput
        {
            Name = parameterName,
            InputType = InputType.SecretText,
            Label = p.Name,
            Placeholder = "Enter token secret:secret"
        });
}

IResourceBuilder<ParameterResource> CreateOpenAiApiKeyParameter()
{
    const string parameterName = "openai-api-key";
    var configValue = builder.Configuration.GetValue<string>(parameterName);
    if (configValue is not null)
    {
        return builder.AddParameter(parameterName, configValue);
    }

    return builder.AddParameter(parameterName, secret: true)
        .WithDescription("OpenAI API Key.")
        .WithCustomInput(p => new InteractionInput
        {
            Name = parameterName,
            InputType = InputType.SecretText,
            Label = p.Name,
            Placeholder = "Enter api key"
        });
}