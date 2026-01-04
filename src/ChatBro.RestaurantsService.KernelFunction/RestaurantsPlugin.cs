using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using ChatBro.RestaurantsService.Model;
using Microsoft.Extensions.DependencyInjection;

namespace ChatBro.RestaurantsService.KernelFunction;

public class RestaurantsPlugin
{
    [Description("""
                 Retrieves nearby restaurants for the specified date and location, returns a CSV string (one restaurant per line).
                 Columns (in order):
                 - Name: restaurant name.
                 - MenuSummary: semicolon-separated menu item names. Each menu item may have diet flags in the end.
                                Lactose-free items are marked with [L],
                                Gluten-free items with [G].
                 - Messages: pipe-separated messages
                 
                 Notes: No header row is included. Fields containing commas, quotes, or newlines are double-quoted
                 and inner quotes are escaped by doubling them. Use this CSV as structured input for further prompt processing.
                 """)]
    public static async Task<string> GetRestaurants(
        [Description("The day on which to find information in ISO 8601 format (YYYY-MM-DD).")] string date,
        [Description("The latitude coordinate for the location.")] double latitude,
        [Description("The longitude coordinate for the location.")] double longitude,
        IServiceProvider serviceProvider)
    {
        var client = serviceProvider.GetRequiredService<RestaurantsServiceClient>();
        using var span = Activity.Current?.Source.StartActivity();

        if (!DateOnly.TryParse(date, out var dateOnly))
        {
            throw new ArgumentException($"Invalid date format: '{date}'. Expected ISO 8601 format (YYYY-MM-DD).", nameof(date));
        }

        var restaurants = await client.GetRestaurantsAsync(dateOnly, latitude, longitude);

        return SerializeCsv(restaurants);
    }

    private static string SerializeCsv(IReadOnlyList<Restaurant> restaurants)
    {
        var sb = new StringBuilder();

        // No header row is included in the CSV output (intentionally omitted to save tokens).
        foreach (var r in restaurants)
        {
            var menuSummary = r.MenuItems is null || r.MenuItems.Count == 0
                ? string.Empty
                : string.Join(';', r.MenuItems.Select(MenuItemToCsv));

            var messages = r.Messages is null || r.Messages.Count == 0
                ? string.Empty
                : string.Join('|', r.Messages);

            sb.Append(EscapeCsv(r.Name));
            sb.Append(',');
            sb.Append(EscapeCsv(menuSummary));
            sb.Append(',');
            sb.Append(EscapeCsv(messages));
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string MenuItemToCsv(RestaurantMenuItem item)
    {
        var lactoFree = item.LactoseFree ? " [L]" : string.Empty;
        var glutenFree = item.GlutenFree ? " [G]" : string.Empty;
        return item.Name + lactoFree + glutenFree;
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
        {
            return '"' + value.Replace("\"", "\"\"") + '"';
        }
        return value;
    }
}