using ChatBro.RestaurantsService.Model;

namespace ChatBro.RestaurantsService.Clients
{
    public class LounaatClient(
        LounaatScrapper scrapper,
        LounaatParser parser,
        ILogger<LounaatClient> logger)
    {
        public async Task<IList<Restaurant>> GetRestaurants(DateOnly date)
        {
            logger.LogInformation("Get restaurants from site for date {Date}", date);
            var html = await scrapper.Scrape(date);
            var restaurants = parser.Parse(html);
            logger.LogDebug("Found {Count} restaurants", restaurants.Count);
            return restaurants;
        }
    }
}
