using ChatBro.AiService.DependencyInjection;
using ChatBro.AiService.Options;
using ChatBro.AiService.Services;
using ChatBro.RestaurantsService.KernelFunction;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOptions<ChatHistorySettings>()
    .BindConfiguration("Chat:History")
    .ValidateDataAnnotations();

builder.Services
    .AddMemoryCache()
    .AddScoped<ChatService>()
    .AddScoped<IContextProvider, ContextProvider>()
    .AddControllers();

builder.Services.AddHttpClient<RestaurantsServiceClient>(
    static client => client.BaseAddress = new Uri("https+http://chatbro-restaurants"));

builder.AddSemanticKernel();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
