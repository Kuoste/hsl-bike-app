namespace HslBikeApp.Models;

/// A snapshot of all station bike counts at a point in time.
/// Used for trend tracking (both by the GH Actions poller and client-side).
public record StationSnapshot
{
    public DateTime Timestamp { get; init; }

    /// Station ID → bikes available at that moment.
    public Dictionary<string, int> BikeCounts { get; init; } = new();
}

public enum AvailabilityTrend
{
    RapidDecrease,
    Decreasing,
    Stable,
    Increasing,
    RapidIncrease
}
