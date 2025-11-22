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


var paperlessUrl = CreateUiParameter("paperless-url", 
    description: "Paperless-NGX URL.", 
    placeholder: "http://paperless:8000",
    inputType: InputType.Text);
var paperlessApiKey = CreateUiParameter(
    "paperless-api-key", description: "Paperless-NGX API Key.", placeholder: "Enter api key");
var paperlessMcpServer = builder.AddPaperlessMcp("paperless-mcp", paperlessUrl, paperlessApiKey);


var telegramToken = CreateUiParameter(
    "telegram-token", description: "Telegram bot token.", placeholder: "Enter token secret:secret");
var openAiApiKey = CreateUiParameter(
    "openai-api-key", description: "OpenAI API Key.", placeholder: "Enter api key");
var openAi = builder.AddOpenAI("openai")
    .WithApiKey(openAiApiKey);
var openAiModel = builder.AddParameter("openai-model", value: "gpt-5-nano", publishValueAsDefault: true);
var server = builder.AddProject<ChatBro_Server>("chatbro-server")
    .WithReference(openAi)
    .WithEnvironment("Chat__AiModel", openAiModel)
    .WithEnvironment("OPENAI_EXPERIMENTAL_ENABLE_OPEN_TELEMETRY", true.ToString())
    .WithReference(redis).WaitFor(redis)
    .WithReference(restaurants).WaitFor(restaurants)
    .WithEnvironment("Telegram__Token", telegramToken)
    .WithReference(paperlessMcpServer).WaitFor(paperlessMcpServer);


builder.Build().Run();

return;

IResourceBuilder<ParameterResource> CreateUiParameter(string name, string description, 
    string? placeholder = null,
    InputType inputType = InputType.SecretText)
{
    var configValue = builder.Configuration.GetValue<string>(name);
    if (configValue is not null)
    {
        return builder.AddParameter(name, configValue);
    }

    var isSecret = inputType == InputType.SecretText;
    return builder.AddParameter(name, secret: isSecret)
        .WithDescription(description)
        .WithCustomInput(p => new InteractionInput
        {
            Name = name,
            InputType = inputType,
            Label = p.Name,
            Placeholder = placeholder
        });
}