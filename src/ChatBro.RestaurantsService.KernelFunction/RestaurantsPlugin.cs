using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using ChatBro.RestaurantsService.Model;
using Microsoft.SemanticKernel;

namespace ChatBro.RestaurantsService.KernelFunction;

public class RestaurantsPlugin(RestaurantsServiceClient client)
{
    [KernelFunction("get_restaurants")]
    [Description("""
                 Retrieves nearby restaurants for the specified date and returns a CSV string (one restaurant per line).
                 Columns (in order):
                 - Name: restaurant name
                 - MenuSummary: semicolon-separated menu item names
                 - Messages: pipe-separated messages
                 
                 Notes: No header row is included. Fields containing commas, quotes, or newlines are double-quoted 
                 and inner quotes are escaped by doubling them. Use this CSV as structured input for further prompt processing.
                 """)]
    public async Task<string> GetRestaurants(
        [Description("The day on which to find information.")] DateTime dateTime)
    {
        using var span = Activity.Current?.Source.StartActivity();

        var date = DateOnly.FromDateTime(dateTime);
        var restaurants = await client.GetRestaurantsAsync(date);

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
                : string.Join(';', r.MenuItems.Select(mi => mi.Name));

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