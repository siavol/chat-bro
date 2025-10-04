using System.ComponentModel;
using System.Diagnostics;
using ChatBro.RestaurantsService.Model;
using Microsoft.SemanticKernel;

namespace ChatBro.RestaurantsService.KernelFunction;

public class RestaurantsPlugin(RestaurantsServiceClient client)
{
    [KernelFunction("get_restaurants")]
    [Description("Retrieves the list of nearby restaurants with daily menu for the given date.")]
    public async Task<Restaurant[]> GetRestaurants(DateOnly date)
    {
        using var span = Activity.Current?.Source.StartActivity();

        var restaurants = await client.GetRestaurantsAsync(date);
        return restaurants
            .Where(RestaurantsHasMenuItems)
            .ToArray();
    }

    private static bool RestaurantsHasMenuItems(Restaurant r) => r.MenuItems.Count > 0;
}