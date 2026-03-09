using System.Text.Json;
using HslBikeApp.Models;

namespace HslBikeApp.Services;

public class CycleLaneService
{
    private const string DefaultUrl =
        "https://kartta.hel.fi/ws/geoserver/avoindata/wfs"
        + "?service=WFS&version=2.0.0&request=GetFeature"
        + "&typeName=avoindata:Pyoratieverkko"
        + "&outputFormat=application/json"
        + "&srsName=EPSG:4326";

    private readonly HttpClient _http;
    private readonly string _url;

    public CycleLaneService(HttpClient http, string? url = null)
    {
        _http = http;
        _url = url ?? DefaultUrl;
    }

    public async Task<List<CycleLane>> FetchCycleLanesAsync()
    {
        var response = await _http.GetAsync(_url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        var features = doc.RootElement.GetProperty("features");
        var lanes = new List<CycleLane>();

        foreach (var feature in features.EnumerateArray())
        {
            var geometry = feature.GetProperty("geometry");
            var geoType = geometry.GetProperty("type").GetString();
            if (geoType != "LineString") continue;

            var props = feature.TryGetProperty("properties", out var p) ? p : default;
            var coords = geometry.GetProperty("coordinates");

            var coordinates = new List<double[]>();
            foreach (var coord in coords.EnumerateArray())
            {
                var lon = coord[0].GetDouble();
                var lat = coord[1].GetDouble();
                coordinates.Add([lat, lon]); // Flip to [lat, lon]
            }

            lanes.Add(new CycleLane
            {
                Id = props.ValueKind != JsonValueKind.Undefined
                    && props.TryGetProperty("id", out var id) ? id.ToString() : "",
                Name = props.ValueKind != JsonValueKind.Undefined
                    && props.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String
                    ? name.GetString() ?? "" : "",
                Surface = props.ValueKind != JsonValueKind.Undefined
                    && props.TryGetProperty("surface", out var surface) && surface.ValueKind == JsonValueKind.String
                    ? surface.GetString() ?? "unknown" : "unknown",
                Coordinates = coordinates
            });
        }

        return lanes;
    }
}
