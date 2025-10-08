using ChatBro.RestaurantsService.Model;
using Microsoft.Extensions.Caching.Memory;

namespace ChatBro.RestaurantsService.Clients
{
    public class LounaatClient(
        LounaatScrapper scrapper,
        LounaatParser parser,
        IMemoryCache cache,
        ILogger<LounaatClient> logger)
    {
        private static readonly TimeSpan DefaultTtl = TimeSpan.FromDays(1);

        public async Task<IList<Restaurant>> GetRestaurants(DateOnly date)
        {
            var cacheKey = $"lounaat_{date:yyyy-MM-dd}";
            if (cache.TryGetValue(cacheKey, out IList<Restaurant>? cachedRestaurants) && cachedRestaurants is not null)
            {
                logger.LogInformation("Returning cached restaurants for date {Date}", date);
                return cachedRestaurants;
            }

            logger.LogInformation("Get restaurants from site for date {Date}", date);
            var html = await scrapper.Scrape(date);
            var restaurants = parser.Parse(html);
            logger.LogDebug("Found {Count} restaurants", restaurants.Count);

            cache.Set(cacheKey, restaurants, DefaultTtl);
            return restaurants;
        }
    }
}
