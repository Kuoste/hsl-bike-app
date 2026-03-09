using System.Text.Json;
using HslBikeApp.Models;

namespace HslBikeApp.Services;

public class CycleLaneService
{
    private const string LegacyUrl =
        "https://kartta.hel.fi/ws/geoserver/avoindata/wfs"
        + "?service=WFS&version=2.0.0&request=GetFeature"
        + "&typeName=avoindata:Pyoratieverkko"
        + "&outputFormat=application/json"
        + "&srsName=EPSG:4326";

    private const string CurrentUrl =
        "https://kartta.hel.fi/ws/geoserver/avoindata/wfs"
        + "?service=WFS&version=2.0.0&request=GetFeature"
        + "&typeNames=avoindata:Yleiskaava2016_liikenneverkko"
        + "&outputFormat=application/json"
        + "&srsName=EPSG:4326"
        + "&CQL_FILTER=selite_fi%20%3D%20%27Baanaverkko%27";

    private readonly HttpClient _http;
    private readonly string _url;

    public CycleLaneService(HttpClient http, string? url = null)
    {
        _http = http;
        _url = url ?? CurrentUrl;
    }

    public async Task<List<CycleLane>> FetchCycleLanesAsync()
    {
        Exception? lastError = null;
        foreach (var requestUrl in GetCandidateUrls())
        {
            try
            {
                var response = await _http.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("features", out var features))
                    continue;

                var lanes = ParseCycleLanes(features);
                if (lanes.Count > 0)
                    return lanes;
            }
            catch (Exception ex)
            {
                lastError = ex;
            }
        }

        throw lastError ?? new InvalidOperationException("No cycle lane data sources returned usable features.");
    }

    private IEnumerable<string> GetCandidateUrls()
    {
        yield return _url;
        if (!string.Equals(_url, CurrentUrl, StringComparison.Ordinal))
            yield return CurrentUrl;
        if (!string.Equals(_url, LegacyUrl, StringComparison.Ordinal))
            yield return LegacyUrl;
    }

    private static List<CycleLane> ParseCycleLanes(JsonElement features)
    {
        var lanes = new List<CycleLane>();

        foreach (var feature in features.EnumerateArray())
        {
            if (!feature.TryGetProperty("geometry", out var geometry))
                continue;

            var geoType = geometry.GetProperty("type").GetString();
            if (geoType is not ("LineString" or "MultiLineString"))
                continue;

            var props = feature.TryGetProperty("properties", out var p) ? p : default;
            foreach (var coordinates in ReadCoordinateSets(geometry))
            {
                if (coordinates.Count == 0)
                    continue;

                lanes.Add(new CycleLane
                {
                    Id = props.ValueKind != JsonValueKind.Undefined
                        && props.TryGetProperty("id", out var id) ? id.ToString() : "",
                    Name = GetString(props, "name")
                        ?? GetString(props, "selite_fi")
                        ?? GetString(props, "paikka")
                        ?? "Cycle lane",
                    Surface = GetString(props, "surface")
                        ?? GetString(props, "kohdetyyppi")
                        ?? "unknown",
                    Coordinates = coordinates
                });
            }
        }

        return lanes;
    }

    private static IEnumerable<List<double[]>> ReadCoordinateSets(JsonElement geometry)
    {
        var coords = geometry.GetProperty("coordinates");
        var geoType = geometry.GetProperty("type").GetString();

        if (geoType == "LineString")
        {
            yield return ReadCoordinateList(coords);
            yield break;
        }

        foreach (var line in coords.EnumerateArray())
            yield return ReadCoordinateList(line);
    }

    private static List<double[]> ReadCoordinateList(JsonElement coords)
    {
        var coordinates = new List<double[]>();
        foreach (var coord in coords.EnumerateArray())
        {
            if (coord.GetArrayLength() < 2)
                continue;

            var lon = coord[0].GetDouble();
            var lat = coord[1].GetDouble();
            coordinates.Add([lat, lon]);
        }

        return coordinates;
    }

    private static string? GetString(JsonElement props, string propertyName)
    {
        if (props.ValueKind == JsonValueKind.Undefined)
            return null;
        if (!props.TryGetProperty(propertyName, out var value))
            return null;
        return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
    }
}
