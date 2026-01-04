using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using ChatBro.RestaurantsService.Model;

namespace ChatBro.RestaurantsService.KernelFunction;

public class RestaurantsServiceClient(HttpClient httpClient, JsonSerializerOptions? jsonOptions = null)
{
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly JsonSerializerOptions _jsonOptions = jsonOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Retrieves the list of restaurants from the restaurants service.
    /// </summary>
    /// <param name="date"></param>
    /// <param name="latitude">Latitude coordinate for location-based search.</param>
    /// <param name="longitude">Longitude coordinate for location-based search.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of restaurants (possibly empty).</returns>
    public async Task<IReadOnlyList<Restaurant>> GetRestaurantsAsync(
        DateOnly? date,
        double latitude,
        double longitude,
        CancellationToken cancellationToken = default)
    {
        date ??= DateOnly.FromDateTime(DateTime.Now);
        
        var dateParam = Uri.EscapeDataString(date.Value.ToString("o"));
        var latParam = Uri.EscapeDataString(latitude.ToString(CultureInfo.InvariantCulture));
        var lngParam = Uri.EscapeDataString(longitude.ToString(CultureInfo.InvariantCulture));
        var requestUri = $"lounaat?date={dateParam}&lat={latParam}&lng={lngParam}";
        
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<LounaatResponse>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Unable to deserialize lounaat restaurants.");
        return payload.Restaurants;
    }
}
