using ChatBro.AiService.DependencyInjection;
using ChatBro.AiService.Services;
using ChatBro.RestaurantsService.KernelFunction;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddRedisClient(connectionName: "redis");

builder.Services
    .AddScoped<ChatService>()
    .AddControllers();

builder.Services.AddHttpClient<RestaurantsServiceClient>(
    static client => client.BaseAddress = new Uri("https+http://chatbro-restaurants"));

builder.AddAgents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
