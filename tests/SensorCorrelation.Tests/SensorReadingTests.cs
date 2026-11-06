namespace SensorCorrelation.Tests;

// detecting coordinates out of bounds, including the edges
public class SensorReadingTests
{
    [Theory]
    [InlineData(51.5, -114.4)]   // Calgary
    [InlineData(90, 180)]        // edges count as valid
    [InlineData(-90, -180)]
    [InlineData(0, 0)]
    public void ValidCoordinates_AreAccepted(double lat, double lon)
    {
        Assert.True(new SensorReading(1, lat, lon).HasValidCoordinates());
    }

    [Theory]
    [InlineData(90.0001, 0)]     // only slightly too far north
    [InlineData(-90.0001, 0)]
    [InlineData(0, 180.0001)]
    [InlineData(0, -180.0001)]
    [InlineData(95.29, -145.4)]  // like sensor 2 ID 43: only the latitude is wrong
    [InlineData(23.4, 182.4)]    // like sensor 1 ID 51: only the longitude is wrong
    [InlineData(double.NaN, 0)]  // "not a number" fails every comparison
    [InlineData(0, double.PositiveInfinity)]
    public void InvalidCoordinates_AreRejected(double lat, double lon)
    {
        Assert.False(new SensorReading(1, lat, lon).HasValidCoordinates());
    }
}
