using HslBikeApp.Models;

namespace HslBikeApp.Tests.Models;

public class BikeStationTests
{
    [Fact]
    public void Occupancy_ReturnsCorrectRatio()
    {
        var station = new BikeStation { Id = "1", Name = "Test", Capacity = 20, BikesAvailable = 15 };
        Assert.Equal(0.75, station.Occupancy);
    }

    [Fact]
    public void Occupancy_ZeroCapacity_ReturnsZero()
    {
        var station = new BikeStation { Id = "1", Name = "Test", Capacity = 0, BikesAvailable = 0 };
        Assert.Equal(0, station.Occupancy);
    }

    [Fact]
    public void IsEmpty_WhenNoBikes()
    {
        var station = new BikeStation { Id = "1", Name = "Test", BikesAvailable = 0 };
        Assert.True(station.IsEmpty);
    }

    [Fact]
    public void IsEmpty_WhenBikesAvailable_ReturnsFalse()
    {
        var station = new BikeStation { Id = "1", Name = "Test", BikesAvailable = 5 };
        Assert.False(station.IsEmpty);
    }

    [Fact]
    public void IsFull_WhenNoSpaces()
    {
        var station = new BikeStation { Id = "1", Name = "Test", SpacesAvailable = 0 };
        Assert.True(station.IsFull);
    }

    [Fact]
    public void IsFull_WhenSpacesAvailable_ReturnsFalse()
    {
        var station = new BikeStation { Id = "1", Name = "Test", SpacesAvailable = 3 };
        Assert.False(station.IsFull);
    }

    [Fact]
    public void DefaultValues_AreCorrect()
    {
        var station = new BikeStation { Id = "1", Name = "Test" };
        Assert.Equal("", station.Address);
        Assert.True(station.IsActive);
        Assert.Null(station.LastUpdated);
        Assert.Equal(0, station.Capacity);
    }
}
