namespace ChatBro.E2E.Tests.StepDefinitions;

[Binding]
public class HttpStepDefinitions(HttpContext httpContext)
{
    private HttpResponseMessage? _response;

    [When("request is (GET|POST|PUT|DELETE|PATCH|HEAD|OPTIONS) (.*)")]
    public async Task WhenRequestIsGetHealth(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        _response = await httpContext.Client.SendAsync(request);
    }
    
    [Then("the response status is {int}")]
    public void ThenResponseStatusIs(int statusCode)
    {
        Assert.NotNull(_response);
        Assert.Equal(statusCode,  (int)_response.StatusCode);
    }

    [StepArgumentTransformation]
    public HttpMethod HttpMethodTransform(string method) => HttpMethod.Parse(method);
}