using ChatBro.RestaurantsService.Model;
using HtmlAgilityPack;

namespace ChatBro.RestaurantsService.Clients;

public class LounaatParser(ILogger<LounaatParser> logger)
{
    public IList<Restaurant> Parse(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        
        var restaurants = new List<Restaurant>();
        
        foreach (var itemNode in doc.DocumentNode.SelectNodes("//div[contains(@class,'menu item')]"))
        {
            var nameNode = itemNode.SelectSingleNode(".//h3/a");
            var name = nameNode?.InnerText.Trim() ?? "";

            var distanceNode = itemNode.SelectSingleNode(".//p[@class='dist']");
            var distanceText = distanceNode?.InnerText.Trim().Replace("m", "") ?? "0";
            var distance = double.TryParse(distanceText, out var d) ? d : 0;

            var menuItems = new List<RestaurantMenuItem>();
            foreach (var dishNode in itemNode.SelectNodes(".//li[contains(@class,'menu-item')]") ?? Enumerable.Empty<HtmlNode>())
            {
                var dishNameNode = dishNode.SelectSingleNode(".//p[contains(@class,'dish')]");
                if (dishNameNode == null) continue;
                var dishName = dishNameNode.InnerText.Trim();

                // Проверяем диетические метки
                bool lakto = dishNode.SelectSingleNode(".//a[contains(@class,'diet-l')]") != null;
                bool maito = dishNode.SelectSingleNode(".//a[contains(@class,'diet-m')]") != null;
                bool glute = dishNode.SelectSingleNode(".//a[contains(@class,'diet-g')]") != null;

                menuItems.Add(new RestaurantMenuItem(dishName, lakto, maito, glute));
            }

            // Дополнительные сообщения (например ссылки или текст без диет. меток)
            var messages = new List<string>();
            foreach (var li in itemNode.SelectNodes(".//li[contains(@class,'menu-item') and not(contains(@class,'item-diet'))]") ?? Enumerable.Empty<HtmlNode>())
            {
                var text = li.InnerText.Trim();
                if (!string.IsNullOrEmpty(text))
                    messages.Add(text);
            }

            restaurants.Add(new Restaurant(name, menuItems, distance, messages));
        }

        return restaurants;
    }
}