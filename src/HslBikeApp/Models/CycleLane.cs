namespace HslBikeApp.Models;

public record CycleLane
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Surface { get; init; } = "unknown";

    /// Ordered list of [latitude, longitude] pairs.
    public required List<double[]> Coordinates { get; init; }

    /// Parse from a GeoJSON Feature, flipping [lon,lat] to [lat,lon].
    public static CycleLane FromGeoJsonFeature(Dictionary<string, object?> feature)
    {
        var props = feature.TryGetValue("properties", out var p) && p is Dictionary<string, object?> pd
            ? pd : new Dictionary<string, object?>();

        var geometry = feature["geometry"] as Dictionary<string, object?>
            ?? throw new ArgumentException("Feature missing geometry");

        var rawCoords = geometry["coordinates"] as List<object?>
            ?? throw new ArgumentException("Geometry missing coordinates");

        var coordinates = rawCoords.Select(c =>
        {
            var pair = c as List<object?> ?? throw new ArgumentException("Invalid coordinate");
            var lon = Convert.ToDouble(pair[0]);
            var lat = Convert.ToDouble(pair[1]);
            return new[] { lat, lon }; // Flip to [lat, lon]
        }).ToList();

        return new CycleLane
        {
            Id = props.TryGetValue("id", out var id) ? id?.ToString() ?? "" : "",
            Name = props.TryGetValue("name", out var name) ? name?.ToString() ?? "" : "",
            Surface = props.TryGetValue("surface", out var surface) ? surface?.ToString() ?? "unknown" : "unknown",
            Coordinates = coordinates
        };
    }
}
