using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

// FetchSnapshot: Called by GitHub Actions every 5 minutes.
// Fetches GBFS station status, appends a snapshot to data/snapshots.json,
// and trims to the last 30 entries (~2.5 hours).

const int MaxSnapshots = 30;
const string StationStatusUrl = "https://api.digitransit.fi/routing/v2/hsl/bike-rental/gbfs/v2/station_status";

var apiKey = Environment.GetEnvironmentVariable("DIGITRANSIT_SUBSCRIPTION_KEY");
if (string.IsNullOrEmpty(apiKey))
{
    Console.Error.WriteLine("Error: DIGITRANSIT_SUBSCRIPTION_KEY environment variable not set.");
    return 1;
}

var snapshotPath = args.Length > 0 ? args[0] : "data/snapshots.json";
var dir = Path.GetDirectoryName(snapshotPath);
if (!string.IsNullOrEmpty(dir))
    Directory.CreateDirectory(dir);

var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false
};

using var http = new HttpClient();
http.DefaultRequestHeaders.Add("digitransit-subscription-key", apiKey);

Console.WriteLine("Fetching station status...");
var response = await http.GetAsync(StationStatusUrl);
response.EnsureSuccessStatusCode();

var statusDoc = await response.Content.ReadFromJsonAsync<JsonDocument>();
var stations = statusDoc?.RootElement.GetProperty("data").GetProperty("stations");

if (stations is null)
{
    Console.Error.WriteLine("Error: No station data in response.");
    return 1;
}

var bikeCounts = new Dictionary<string, int>();
foreach (var station in stations.Value.EnumerateArray())
{
    var stationId = station.GetProperty("station_id").GetString() ?? "";
    var bikes = station.GetProperty("num_bikes_available").GetInt32();
    bikeCounts[stationId] = bikes;
}

Console.WriteLine($"Got bike counts for {bikeCounts.Count} stations.");

// Load existing snapshots
List<SnapshotEntry> snapshots;
if (File.Exists(snapshotPath))
{
    var existing = await File.ReadAllTextAsync(snapshotPath);
    snapshots = JsonSerializer.Deserialize<List<SnapshotEntry>>(existing, jsonOptions) ?? [];
}
else
{
    snapshots = [];
}

// Append new snapshot
snapshots.Add(new SnapshotEntry
{
    Timestamp = DateTime.UtcNow,
    BikeCounts = bikeCounts
});

// Trim to last N entries
if (snapshots.Count > MaxSnapshots)
    snapshots = snapshots.Skip(snapshots.Count - MaxSnapshots).ToList();

// Write back
await File.WriteAllTextAsync(snapshotPath, JsonSerializer.Serialize(snapshots, jsonOptions));
Console.WriteLine($"Wrote {snapshots.Count} snapshots to {snapshotPath}.");

return 0;

record SnapshotEntry
{
    public DateTime Timestamp { get; init; }
    public Dictionary<string, int> BikeCounts { get; init; } = new();
}
