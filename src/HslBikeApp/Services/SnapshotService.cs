using System.Net;
using System.Net.Http.Json;
using HslBikeApp.Models;

namespace HslBikeApp.Services;

public class SnapshotService
{
    private readonly HttpClient _http;
    private readonly string _snapshotUrl;

    public SnapshotService(HttpClient http, string snapshotUrl)
    {
        _http = http;
        _snapshotUrl = snapshotUrl;
    }

    /// Fetch pre-built snapshots from GitHub Pages (created by the Actions poller).
    public async Task<List<StationSnapshot>> FetchSnapshotsAsync()
    {
        try
        {
            var response = await _http.GetAsync(_snapshotUrl);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return [];

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<StationSnapshot>>() ?? [];
        }
        catch (HttpRequestException)
        {
            return [];
        }
    }
}
