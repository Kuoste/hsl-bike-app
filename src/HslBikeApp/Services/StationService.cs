using System.Net.Http.Json;
using System.Text.Json;
using HslBikeApp.Models;

namespace HslBikeApp.Services;

public class StationService
{
    private const string StationInfoUrl =
        "https://api.digitransit.fi/routing/v2/hsl/bike-rental/gbfs/v2/station_information";
    private const string StationStatusUrl =
        "https://api.digitransit.fi/routing/v2/hsl/bike-rental/gbfs/v2/station_status";

    private readonly HttpClient _http;

    public StationService(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<BikeStation>> FetchStationsAsync()
    {
        var infoTask = _http.GetFromJsonAsync<JsonDocument>(StationInfoUrl);
        var statusTask = _http.GetFromJsonAsync<JsonDocument>(StationStatusUrl);

        await Task.WhenAll(infoTask, statusTask);

        var infoDoc = await infoTask ?? throw new InvalidOperationException("No station info response");
        var statusDoc = await statusTask ?? throw new InvalidOperationException("No station status response");

        var infoStations = infoDoc.RootElement.GetProperty("data").GetProperty("stations");
        var statusStations = statusDoc.RootElement.GetProperty("data").GetProperty("stations");

        // Build lookup: stationId -> status
        var statusMap = new Dictionary<string, JsonElement>();
        foreach (var s in statusStations.EnumerateArray())
        {
            var id = s.GetProperty("station_id").GetString() ?? "";
            statusMap[id] = s;
        }

        var stations = new List<BikeStation>();
        foreach (var info in infoStations.EnumerateArray())
        {
            var id = info.GetProperty("station_id").GetString() ?? "";
            statusMap.TryGetValue(id, out var status);

            stations.Add(new BikeStation
            {
                Id = id,
                Name = info.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                Address = info.TryGetProperty("address", out var a) ? a.GetString() ?? "" : "",
                Latitude = info.GetProperty("lat").GetDouble(),
                Longitude = info.GetProperty("lon").GetDouble(),
                Capacity = info.TryGetProperty("capacity", out var c) ? c.GetInt32() : 0,
                BikesAvailable = status.ValueKind != JsonValueKind.Undefined
                    && status.TryGetProperty("num_bikes_available", out var b) ? b.GetInt32() : 0,
                SpacesAvailable = status.ValueKind != JsonValueKind.Undefined
                    && status.TryGetProperty("num_docks_available", out var d) ? d.GetInt32() : 0,
                IsActive = status.ValueKind != JsonValueKind.Undefined
                    && status.TryGetProperty("is_renting", out var r) && r.GetBoolean(),
                LastUpdated = status.ValueKind != JsonValueKind.Undefined
                    && status.TryGetProperty("last_reported", out var lr)
                    && lr.ValueKind == JsonValueKind.Number
                        ? DateTimeOffset.FromUnixTimeSeconds(lr.GetInt64()).UtcDateTime
                        : null
            });
        }

        return stations;
    }
}
