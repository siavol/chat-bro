#pragma warning disable ASPIREINTERACTION001

using Microsoft.Extensions.Configuration;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);
builder.AddDockerComposeEnvironment("docker-compose");

var redis = builder.AddRedis("redis")
    .WithRedisInsight()
    .WithDataVolume()
    .WithPersistence(
        interval: TimeSpan.FromMinutes(5),
        keysChangedThreshold: 1
    )
    .WithLifetime(ContainerLifetime.Persistent);

var restaurants = builder.AddProject<ChatBro_RestaurantsService>("chatbro-restaurants");

var openAiApiKey = CreateUiSecretParameter(
    "openai-api-key", description: "OpenAI API Key.", placeholder: "Enter api key");
var openAi = builder.AddOpenAI("openai")
    .WithApiKey(openAiApiKey);
var openAiModel = builder.AddParameter("openai-model", value: "gpt-5-nano", publishValueAsDefault: true);
var aiService = builder.AddProject<ChatBro_AiService>("chatbro-ai-service")
    .WithReference(openAi)
    .WithEnvironment("Chat__AiModel", openAiModel)
    .WithEnvironment("OPENAI_EXPERIMENTAL_ENABLE_OPEN_TELEMETRY", true.ToString())
    .WithReference(redis).WaitFor(redis)
    .WithReference(restaurants).WaitFor(restaurants);


var telegramToken = CreateUiSecretParameter(
    "telegram-token", description: "Telegram bot token.", placeholder: "Enter token secret:secret");
builder.AddProject<ChatBro_TelegramBotService>("chatbro-telegram-bot")
    .WithEnvironment("Telegram__Token", telegramToken)
    .WithReference(aiService).WaitFor(aiService);


builder.Build().Run();

return;

IResourceBuilder<ParameterResource> CreateUiSecretParameter(string name, string description, string? placeholder = null)
{
    var configValue = builder.Configuration.GetValue<string>(name);
    if (configValue is not null)
    {
        return builder.AddParameter(name, configValue);
    }

    return builder.AddParameter(name, secret: true)
        .WithDescription(description)
        .WithCustomInput(p => new InteractionInput
        {
            Name = name,
            InputType = InputType.SecretText,
            Label = p.Name,
            Placeholder = placeholder
        });
}