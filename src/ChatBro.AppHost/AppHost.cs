#pragma warning disable ASPIREINTERACTION001

using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var telegramToken = builder.AddParameter("telegram-token")
    .WithDescription("Telegram bot token.")
    .WithCustomInput(p => new InteractionInput
    {
        InputType = InputType.SecretText,
        Label = p.Name,
        Placeholder = "Enter token secret:secret"
    });

var telegramBot = builder.AddProject<ChatBro_TelegramBotService>("telegram-bot")
    .WithEnvironment("Telegram__Token", telegramToken);

builder.Build().Run();
