using ChatBro.RestaurantsService.Clients;

using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services
    .AddSingleton(new ActivitySource("ChatBro.RestaurantsService"))
    .AddMemoryCache()
    .AddTransient<LounaatScrapper>()
    .AddTransient<LounaatParser>()
    .AddTransient<LounaatClient>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
