using ChatBro.RestaurantsService.Clients;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services
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
