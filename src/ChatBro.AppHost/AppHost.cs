using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var telegramBot = builder.AddProject<ChatBro_TelegramBotService>("telegram-bot");

builder.Build().Run();
