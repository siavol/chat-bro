using ChatBro.RestaurantsService.Clients;
using ChatBro.RestaurantsService.Jobs;
using Coravel;

using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services
    .AddSingleton(new ActivitySource("ChatBro.RestaurantsService"))
    .AddMemoryCache()
    .AddScheduler()
    .AddTransient<LounaatScrapper>()
    .AddTransient<LounaatParser>()
    .AddTransient<LounaatClient>()
    .AddTransient<WarmupLounaatCacheJob>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Services.UseScheduler(scheduler =>
{
    // warmup job: weekdays at 09:00
    scheduler
        .Schedule<WarmupLounaatCacheJob>()
        .EveryMinute().Once();
        // .Cron("0 9 * * 1-5");
});

app.Run();
