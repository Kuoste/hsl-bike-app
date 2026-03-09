namespace HslBikeApp.Models;

public record StationHistory
{
    public required string DepartureStationId { get; init; }
    public required string ArrivalStationId { get; init; }
    public string ArrivalStationName { get; init; } = "";
    public int TripCount { get; init; }
    public int AverageDurationSeconds { get; init; }
    public int AverageDistanceMetres { get; init; }

    public string AverageDurationFormatted
    {
        get
        {
            var minutes = AverageDurationSeconds / 60;
            return minutes < 1 ? "<1 min" : $"{minutes} min";
        }
    }
}
