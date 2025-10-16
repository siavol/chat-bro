using ChatBro.RestaurantsService.Clients;
using ChatBro.RestaurantsService.Jobs;
using Coravel;

using System.Diagnostics;
using ChatBro.RestaurantsService.Configuration;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOptions<SchedulerSettings>()
    .BindConfiguration("Scheduler")
    .ValidateOnStart()
    .ValidateDataAnnotations();

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
    var schedulerSettings = app.Services.GetRequiredService<IOptions<SchedulerSettings>>().Value;
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("SchedulerRegistration");

    logger.LogInformation("Registering scheduled job {Job} with cron expression '{Cron}'",
        nameof(WarmupLounaatCacheJob), schedulerSettings.WarmupJobCron);
    scheduler
        .Schedule<WarmupLounaatCacheJob>()
        .Cron(schedulerSettings.WarmupJobCron);
});

app.Run();
