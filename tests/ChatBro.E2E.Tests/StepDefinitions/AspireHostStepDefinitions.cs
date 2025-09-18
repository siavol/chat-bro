using Aspire.Hosting;
using Microsoft.Extensions.Logging;

namespace ChatBro.E2E.Tests.StepDefinitions;

[Binding]
public class AspireHostStepDefinitions(HttpContext httpContext)
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromSeconds(30);
    
    private static DistributedApplication _app = null!;

    private static IEnumerable<string> GetAppHostArgs()
    {
        if (Environment.GetEnvironmentVariable("CHATBRO_TELEGRAM_TOKEN") is { Length: > 0 } telegramToken)
        {
            yield return $"--telegram-token={telegramToken}";
        }
    }

    [BeforeTestRun]
    public static async Task StartApp()
    {
        var cancellationToken = new CancellationTokenSource(StartupTimeout).Token;
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.ChatBro_AppHost>(GetAppHostArgs().ToArray(), cancellationToken);
        
        appHost.Services.AddLogging(logging =>
        {
            logging.SetMinimumLevel(LogLevel.Debug);
            // Override the logging filters from the app's configuration
            logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
            logging.AddFilter("Aspire.", LogLevel.Debug);
        });
        appHost.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });
    
        _app = await appHost.BuildAsync(cancellationToken).WaitAsync(StartupTimeout, cancellationToken);
    }

    [AfterTestRun]
    public static void StopApp()
    {
        _app.Dispose();
    }

    [Given("the application is started")]
    public async Task GivenTheApplicationIsStarted()
    {
        var cancellationToken = new CancellationTokenSource(StartupTimeout).Token;
        await _app.StartAsync(cancellationToken).WaitAsync(StartupTimeout, cancellationToken);
    }
    
    [When("I send HTTP request to the (.*) service")]
    public async Task WhenISendHttpRequestToTheTelegramBotService(string serviceName)
    {
        var cancellationToken = new CancellationTokenSource(StartupTimeout).Token;

        await _app.ResourceNotifications
            .WaitForResourceHealthyAsync(serviceName, cancellationToken)
            .WaitAsync(StartupTimeout, cancellationToken);
        
        var httpClient = _app.CreateHttpClient(serviceName);
        httpContext.InitializeHttpClient(httpClient);
    }
}