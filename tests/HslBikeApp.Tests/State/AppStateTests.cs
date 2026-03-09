using HslBikeApp.Models;
using HslBikeApp.Services;
using HslBikeApp.State;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace HslBikeApp.Tests.State;

public class AppStateTests
{
    private static AppState CreateAppState(
        List<BikeStation>? stations = null,
        List<StationSnapshot>? snapshots = null)
    {
        // Mock StationService via a mock HttpClient
        var stationHandler = new MockHttpHandler();
        if (stations is not null)
        {
            var infoJson = JsonSerializer.Serialize(new
            {
                data = new
                {
                    stations = stations.Select(s => new
                    {
                        station_id = s.Id,
                        name = s.Name,
                        address = s.Address,
                        lat = s.Latitude,
                        lon = s.Longitude,
                        capacity = s.Capacity
                    })
                }
            });
            var statusJson = JsonSerializer.Serialize(new
            {
                data = new
                {
                    stations = stations.Select(s => new
                    {
                        station_id = s.Id,
                        num_bikes_available = s.BikesAvailable,
                        num_docks_available = s.SpacesAvailable,
                        is_renting = s.IsActive,
                        last_reported = (int)(new DateTimeOffset(s.LastUpdated ?? DateTime.UtcNow).ToUnixTimeSeconds())
                    })
                }
            });
            stationHandler.SetResponse("station_information", infoJson);
            stationHandler.SetResponse("station_status", statusJson);
        }

        var stationService = new StationService(new HttpClient(stationHandler) { BaseAddress = new Uri("https://test.local/") });
        var historyService = new HistoryService(new HttpClient(new MockHttpHandler()), "https://test.local");
        var cycleLaneService = new CycleLaneService(new HttpClient(new MockHttpHandler()));

        var snapshotHandler = new MockHttpHandler();
        if (snapshots is not null)
        {
            snapshotHandler.SetResponse("snapshots", JsonSerializer.Serialize(snapshots, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
        }
        var snapshotService = new SnapshotService(new HttpClient(snapshotHandler) { BaseAddress = new Uri("https://test.local/") }, "https://test.local/snapshots.json");

        return new AppState(stationService, historyService, cycleLaneService, snapshotService);
    }

    [Fact]
    public void SetSearchQuery_FiltersStations()
    {
        var state = CreateAppState();
        // Manually set stations via reflection for unit testing
        typeof(AppState).GetProperty("Stations")!.SetValue(state, new List<BikeStation>
        {
            new() { Id = "1", Name = "Kaivopuisto", Address = "Kaivopuisto 1" },
            new() { Id = "2", Name = "Kamppi", Address = "Kamppi 2" },
            new() { Id = "3", Name = "Kallio", Address = "Kallio 3" }
        });

        state.SetSearchQuery("kamp");
        Assert.Single(state.FilteredStations);
        Assert.Equal("Kamppi", state.FilteredStations[0].Name);
    }

    [Fact]
    public void SetSearchQuery_CaseInsensitive()
    {
        var state = CreateAppState();
        typeof(AppState).GetProperty("Stations")!.SetValue(state, new List<BikeStation>
        {
            new() { Id = "1", Name = "Kaivopuisto", Address = "addr" },
            new() { Id = "2", Name = "KAMPPI", Address = "addr" }
        });

        state.SetSearchQuery("kamppi");
        Assert.Single(state.FilteredStations);
    }

    [Fact]
    public void SetSearchQuery_Empty_ReturnsAll()
    {
        var state = CreateAppState();
        typeof(AppState).GetProperty("Stations")!.SetValue(state, new List<BikeStation>
        {
            new() { Id = "1", Name = "A", Address = "" },
            new() { Id = "2", Name = "B", Address = "" }
        });

        state.SetSearchQuery("");
        Assert.Equal(2, state.FilteredStations.Count);
    }

    [Fact]
    public void GetTrend_WithFewSnapshots_ReturnsStable()
    {
        var state = CreateAppState();
        Assert.Equal(AvailabilityTrend.Stable, state.GetTrend("001"));
    }

    [Fact]
    public void GetTrend_DecreasingBikes_ReturnsDecrease()
    {
        var state = CreateAppState();
        // Inject snapshots showing decrease
        var snapshots = new List<StationSnapshot>();
        var baseTime = DateTime.UtcNow.AddMinutes(-30);
        for (int i = 0; i < 6; i++)
        {
            snapshots.Add(new StationSnapshot
            {
                Timestamp = baseTime.AddMinutes(i * 5),
                BikeCounts = new Dictionary<string, int> { ["001"] = 20 - i * 4 }
            });
        }

        // Use reflection to set _snapshots
        var field = typeof(AppState).GetField("_snapshots", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        field.SetValue(state, snapshots);

        var trend = state.GetTrend("001");
        Assert.True(trend == AvailabilityTrend.Decreasing || trend == AvailabilityTrend.RapidDecrease);
    }

    [Fact]
    public void GetSparkline_ReturnsLastNCounts()
    {
        var state = CreateAppState();
        var snapshots = new List<StationSnapshot>();
        for (int i = 0; i < 15; i++)
        {
            snapshots.Add(new StationSnapshot
            {
                Timestamp = DateTime.UtcNow.AddMinutes(-15 + i),
                BikeCounts = new Dictionary<string, int> { ["001"] = i }
            });
        }

        var field = typeof(AppState).GetField("_snapshots", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        field.SetValue(state, snapshots);

        var sparkline = state.GetSparkline("001", 5);
        Assert.Equal(5, sparkline.Count);
        Assert.Equal(new List<int> { 10, 11, 12, 13, 14 }, sparkline);
    }

    [Fact]
    public void ClearSelection_ResetsState()
    {
        var state = CreateAppState();
        state.ClearSelection();
        Assert.Null(state.SelectedStation);
        Assert.Empty(state.History);
    }

    [Fact]
    public void OnStateChanged_IsRaised()
    {
        var state = CreateAppState();
        var raised = false;
        state.OnStateChanged += () => raised = true;

        state.SetSearchQuery("test");
        Assert.True(raised);
    }

    /// Simple HttpMessageHandler mock for tests
    private class MockHttpHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, string> _responses = new();

        public void SetResponse(string urlContains, string json)
        {
            _responses[urlContains] = json;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri?.ToString() ?? "";
            foreach (var kvp in _responses)
            {
                if (url.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(kvp.Value, System.Text.Encoding.UTF8, "application/json")
                    });
                }
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
