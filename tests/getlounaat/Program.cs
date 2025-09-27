// Program.cs
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    static async Task<int> Main()
    {
        // Настройка CookieContainer и HttpClientHandler
        var cookieContainer = new CookieContainer();
        var handler = new HttpClientHandler
        {
            CookieContainer = cookieContainer,
            UseCookies = true,
            AllowAutoRedirect = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };

        using var http = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(20)
        };

        // Заголовки "как у браузера"
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
            "AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        http.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
        http.DefaultRequestHeaders.AcceptLanguage.ParseAdd("fi-FI,fi;q=0.9,en-US;q=0.8,en;q=0.7");
        http.DefaultRequestHeaders.AcceptEncoding.ParseAdd("gzip, deflate, br");

        // 1) Инициализируем сессию — GET главной страницы, чтобы сервер выставил cookies
        var baseUrl = "https://lounaat.info/";
        Console.WriteLine($"GET {baseUrl} ...");
        var homeResp = await http.GetAsync(baseUrl);
        Console.WriteLine($"Status: {(int)homeResp.StatusCode} {homeResp.ReasonPhrase}");

        // Показать установленные cookie (если есть)
        ShowCookies(cookieContainer, new Uri(baseUrl));

        // 2) Выполняем запрос к API (пример endpoint — скорректируй под конкретный)
        // Пример: common-api.lounaat.info/lounas/pposti/helsinki
        // var apiUrl = "https://www.lounaat.info/ajax/filter?view=lahistolla&day=8&page=0&coords=false";
        // var apiUrl = "https://www.lounaat.info/ajax/filter?view=lahistolla&day=8&page=0&coords[lat]=60.1761732&coords[lng]=24.8359807&coords[address]=keilaniemi-espoo&coords[formattedAddress]=Keilaniemi, Espoo";
        var apiUrl = "https://www.lounaat.info/ajax/filter?view=lahistolla&day=8&page=0&coords[lat]=60.1761732&coords[lng]=24.8359807";
        using var req = new HttpRequestMessage(HttpMethod.Get, apiUrl);

        // Важно: иногда помогает установить Referer/Origin
        req.Headers.Referrer = new Uri("https://lounaat.info/");
        req.Headers.Accept.ParseAdd("application/json, text/javascript, */*; q=0.01");
        req.Headers.Add("X-Requested-With", "XMLHttpRequest"); // если сайт ожидает AJAX
        // Можно также задать дополнительные заголовки, если их видно в DevTools

        Console.WriteLine($"GET {apiUrl} ...");
        var apiResp = await http.SendAsync(req);
        Console.WriteLine($"Status: {(int)apiResp.StatusCode} {apiResp.ReasonPhrase}");

        var content = await apiResp.Content.ReadAsStringAsync();
        Console.WriteLine("Raw response length: " + content.Length);

            // Если HTML — можно вывести отрывок
        Console.WriteLine("HTML snippet:\n" + (content.Length > 1000 ? content[..1000] + "..." : content));

        var indexOfPorsaa = content.IndexOf("porsasta", StringComparison.Ordinal);
        if (indexOfPorsaa != -1)
        {
            var porsaaSnippet = content.Substring(
                Math.Max(0, indexOfPorsaa - 20),
                100);
            Console.WriteLine("PORSAA:" + porsaaSnippet);
        }
        else
        {
            Console.WriteLine("Porsaa not found");
        }

        return 0;
    }

    static void ShowCookies(CookieContainer container, Uri uri)
    {
        Console.WriteLine("Cookies for " + uri.Host + ":");
        var cookies = container.GetCookies(new Uri(uri.Scheme + "://" + uri.Host));
        foreach (Cookie cookie in cookies)
        {
            Console.WriteLine($"- {cookie.Name} = {cookie.Value}; domain={cookie.Domain}; path={cookie.Path}");
        }
    }

    static bool IsLikelyJson(string s)
    {
        s = s?.TrimStart() ?? string.Empty;
        return s.StartsWith("{") || s.StartsWith("[");
    }
}
