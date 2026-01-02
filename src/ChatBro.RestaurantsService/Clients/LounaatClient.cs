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

        public async Task<IList<Restaurant>> GetRestaurants(DateOnly date, double latitude, double longitude, bool ignoreCache = false)
        {
            var cacheKey = new LounaatCacheKey(date, latitude, longitude);
            if (!ignoreCache 
                && cache.TryGetValue(cacheKey, out IList<Restaurant>? cachedRestaurants) 
                && cachedRestaurants is not null)
            {
                logger.LogInformation("Returning cached restaurants for date {Date}", date);
                return cachedRestaurants;
            }

            logger.LogInformation("Get restaurants from site for date {Date}", date);
            var html = await scrapper.Scrape(date, latitude, longitude);
            var restaurants = parser.Parse(html);
            logger.LogDebug("Found {Count} restaurants", restaurants.Count);

            cache.Set(cacheKey, restaurants, DefaultTtl);
            return restaurants;
        }

        private sealed record LounaatCacheKey
        {
            // Quantize coordinates to ~1.1km precision to group nearby locations
            // This reduces cache fragmentation while maintaining location-specific results
            private const double CoordinatePrecision = 0.01;

            public DateOnly Date { get; }
            public double Latitude { get; }
            public double Longitude { get; }

            public LounaatCacheKey(DateOnly date, double latitude, double longitude)
            {
                Date = date;
                Latitude = QuantizeCoordinate(latitude);
                Longitude = QuantizeCoordinate(longitude);
            }

            private static double QuantizeCoordinate(double coordinate)
            {
                return Math.Round(coordinate / CoordinatePrecision) * CoordinatePrecision;
            }
        }
    }
}
