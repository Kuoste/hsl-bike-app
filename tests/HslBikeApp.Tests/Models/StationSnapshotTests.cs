using HslBikeApp.Models;

namespace HslBikeApp.Tests.Models;

public class StationSnapshotTests
{
    [Fact]
    public void Snapshot_StoresTimestampAndCounts()
    {
        var now = DateTime.UtcNow;
        var snapshot = new StationSnapshot
        {
            Timestamp = now,
            BikeCounts = new Dictionary<string, int> { ["001"] = 10, ["002"] = 5 }
        };

        Assert.Equal(now, snapshot.Timestamp);
        Assert.Equal(10, snapshot.BikeCounts["001"]);
        Assert.Equal(5, snapshot.BikeCounts["002"]);
    }

    [Fact]
    public void DefaultBikeCounts_IsEmptyDictionary()
    {
        var snapshot = new StationSnapshot { Timestamp = DateTime.UtcNow };
        Assert.Empty(snapshot.BikeCounts);
    }

    [Fact]
    public void AvailabilityTrend_AllValuesExist()
    {
        var values = Enum.GetValues<AvailabilityTrend>();
        Assert.Equal(5, values.Length);
        Assert.Contains(AvailabilityTrend.RapidDecrease, values);
        Assert.Contains(AvailabilityTrend.Stable, values);
        Assert.Contains(AvailabilityTrend.RapidIncrease, values);
    }
}
