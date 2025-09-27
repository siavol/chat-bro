using ChatBro.TelegramBotService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register a named HttpClient that uses Aspire logical address resolution
builder.Services.AddHttpClient("ai-service",
    static client => client.BaseAddress = new("https+http://ai-service"));

builder.Services.AddOptions<TelegramServiceOptions>()
    .BindConfiguration("Telegram")
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddHostedService<TelegramService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapDefaultEndpoints();


// app.MapGet("/getme", async () =>
// {
//     var me = await bot.GetMe();
//     return $"Hello, World! I am user {me.Id} and my name is {me.FirstName}.";
// });


app.Run();
