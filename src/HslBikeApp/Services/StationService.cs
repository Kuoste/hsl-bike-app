using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using HslBikeApp.Models;

namespace HslBikeApp.Services;

public class StationService
{
    private const string GraphQlUrl =
        "https://api.digitransit.fi/routing/v2/hsl/gtfs/v1";

    private const string Query = """
        {
          vehicleRentalStations {
            stationId
            name
            lat
            lon
            allowPickup
            allowDropoff
            capacity
            availableVehicles { byType { count } }
            availableSpaces   { byType { count } }
          }
        }
        """;

    private readonly HttpClient _http;

    public StationService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<BikeStation>> FetchStationsAsync()
    {
        var payload = JsonSerializer.Serialize(new { query = Query });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await _http.PostAsync(GraphQlUrl, content);
        response.EnsureSuccessStatusCode();

        var doc = await response.Content.ReadFromJsonAsync<JsonDocument>()
            ?? throw new InvalidOperationException("No GraphQL response");

        var rentalStations = doc.RootElement
            .GetProperty("data")
            .GetProperty("vehicleRentalStations");

        var stations = new List<BikeStation>();
        foreach (var s in rentalStations.EnumerateArray())
        {
            var bikesAvailable = SumByType(s, "availableVehicles");
            var spacesAvailable = SumByType(s, "availableSpaces");

            stations.Add(new BikeStation
            {
                Id = s.GetProperty("stationId").GetString() ?? "",
                Name = s.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                Address = "",
                Latitude = s.GetProperty("lat").GetDouble(),
                Longitude = s.GetProperty("lon").GetDouble(),
                Capacity = s.TryGetProperty("capacity", out var cap) ? cap.GetInt32() : 0,
                BikesAvailable = bikesAvailable,
                SpacesAvailable = spacesAvailable,
                IsActive = s.TryGetProperty("allowPickup", out var ap) && ap.GetBoolean(),
            });
        }

        return stations;
    }

    private static int SumByType(JsonElement station, string property)
    {
        if (!station.TryGetProperty(property, out var outer)) return 0;
        if (!outer.TryGetProperty("byType", out var byType)) return 0;
        var total = 0;
        foreach (var entry in byType.EnumerateArray())
        {
            if (entry.TryGetProperty("count", out var c))
                total += c.GetInt32();
        }
        return total;
    }
}
