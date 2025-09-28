using System.Net;

namespace ChatBro.RestaurantsService.Clients;

public class LounaatScrapper(ILogger<LounaatScrapper> logger)
{
    private const string LounaatUrl = "https://lounaat.info/";
    
    public async Task<string> Scrape()
    {
        using var http = GetHttpClient();

        logger.LogInformation("Get main page to grab cookies");
        var homeResp = await http.GetAsync("/");
        
        var apiUrl = "/ajax/filter?view=lahistolla&day=8&page=0&coords[lat]=60.1761732&coords[lng]=24.8359807";
        using var req = new HttpRequestMessage(HttpMethod.Get, apiUrl);

        req.Headers.Referrer = new Uri("https://lounaat.info/");
        req.Headers.Add("X-Requested-With", "XMLHttpRequest");

        logger.LogInformation("Get restaurants menu from filter page");
        var apiResp = await http.SendAsync(req);
        var content = await apiResp.Content.ReadAsStringAsync();
        logger.LogDebug("Raw response length: {ContentLength}", content.Length);

        return content;
    }

    private static HttpClient GetHttpClient()
    {
        var cookieContainer = new CookieContainer();
        var handler = new HttpClientHandler
        {
            CookieContainer = cookieContainer,
            UseCookies = true,
            AllowAutoRedirect = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };

        var http = new HttpClient(handler)
        {
            BaseAddress = new Uri(LounaatUrl),
            Timeout = TimeSpan.FromSeconds(20)
        };

        http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                                                      "AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        http.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("fi-FI,fi;q=0.9,en-US;q=0.8,en;q=0.7");
        http.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate, br");
        
        return http;
    }
}