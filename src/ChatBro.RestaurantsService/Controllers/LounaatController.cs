using ChatBro.RestaurantsService.Clients;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;

namespace ChatBro.RestaurantsService.Controllers;

[ApiController]
[Route("lounaat")]
public class LounaatController(
    LounaatScrapper scrapper,
    ILogger<LounaatController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var html = await scrapper.Scrape();
        // return html;
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        
        var restaurants = new List<object>();
        
        foreach (var itemNode in doc.DocumentNode.SelectNodes("//div[contains(@class,'menu item')]"))
        {
            var nameNode = itemNode.SelectSingleNode(".//h3/a");
            var name = nameNode?.InnerText.Trim() ?? "";

            var distanceNode = itemNode.SelectSingleNode(".//p[@class='dist']");
            var distanceText = distanceNode?.InnerText.Trim().Replace("m", "") ?? "0";
            var distance = double.TryParse(distanceText, out var d) ? d : 0;

            var menuItems = new List<object>();
            foreach (var dishNode in itemNode.SelectNodes(".//li[contains(@class,'menu-item')]") ?? Enumerable.Empty<HtmlNode>())
            {
                var dishNameNode = dishNode.SelectSingleNode(".//p[contains(@class,'dish')]");
                if (dishNameNode == null) continue;
                var dishName = dishNameNode.InnerText.Trim();

                // Проверяем диетические метки
                bool lakto = dishNode.SelectSingleNode(".//a[contains(@class,'diet-l')]") != null;
                bool maito = dishNode.SelectSingleNode(".//a[contains(@class,'diet-m')]") != null;
                bool glute = dishNode.SelectSingleNode(".//a[contains(@class,'diet-g')]") != null;

                menuItems.Add(new { name = dishName, laktositon = lakto, maidoton = maito, gluuteniton = glute });
            }

            // Дополнительные сообщения (например ссылки или текст без диет. меток)
            var messages = new List<string>();
            foreach (var li in itemNode.SelectNodes(".//li[contains(@class,'menu-item') and not(contains(@class,'item-diet'))]") ?? Enumerable.Empty<HtmlNode>())
            {
                var text = li.InnerText.Trim();
                if (!string.IsNullOrEmpty(text))
                    messages.Add(text);
            }

            restaurants.Add(new {
                name,
                menu = menuItems,
                distance,
                messages
            });
        }

        var result = new { restaurants };
        return Ok(result);
    }
}