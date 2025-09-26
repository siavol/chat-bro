using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapDefaultEndpoints();

var tgToken = builder.Configuration["Telegram:Token"] 
              ?? throw new InvalidOperationException("Telegram Token not configured");
var bot = new TelegramBotClient(tgToken);
bot.OnMessage += OnMessage;

app.MapGet("/getme", async () =>
{
    var me = await bot.GetMe();
    return $"Hello, World! I am user {me.Id} and my name is {me.FirstName}.";
});


app.Run();



async Task OnMessage(Message message, UpdateType type)
{
    await bot.SendMessage(message.Chat, $"{message.From} said: {message.Text}. Confirming message received. Channel id: {message.Chat.Id}");
}
