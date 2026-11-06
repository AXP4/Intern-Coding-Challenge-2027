namespace SensorCorrelation.Tests;

// checking the haversine distance against values we know
public class GeoDistanceTests
{
    [Fact]
    public void SamePoint_IsZeroMeters()
    {
        Assert.Equal(0, GeoDistance.Meters(51.5, -114.4, 51.5, -114.4), precision: 6);
    }

    [Fact]
    public void SamplePair_MatchesHandEstimate()
    {
        // sample S1 56 and S2 46: we estimated about 41 m by hand
        double meters = GeoDistance.Meters(51.5565891273184, -114.39568421797782, 51.55658416939388, -114.3950877437552);
        Assert.InRange(meters, 40.5, 42);
    }

    [Fact]
    public void OneDegreeOfLatitude_IsAbout111Km()
    {
        Assert.InRange(GeoDistance.Meters(0, 0, 1, 0), 111_000, 111_400);
    }

    [Fact]
    public void OneDegreeOfLongitude_ShrinksAwayFromTheEquator()
    {
        double atEquator = GeoDistance.Meters(0, 0, 0, 1);
        double atCalgary = GeoDistance.Meters(51.5, 0, 51.5, 1);

        // cos(51.5°) ≈ 0.62, like in our estimate
        Assert.InRange(atCalgary / atEquator, 0.61, 0.63);
    }

    [Fact]
    public void PointsAcrossThe180Line_AreCloseNotAcrossThePlanet()
    {
        // 179.9999 and -179.9999 are only about 22 m apart, on opposite sides of the 180° line
        Assert.InRange(GeoDistance.Meters(0, 179.9999, 0, -179.9999), 20, 25);
    }

    [Fact]
    public void DistanceIsTheSameInBothDirections()
    {
        Assert.Equal(
            GeoDistance.Meters(51.5, -114.4, 36.1, 138.8),
            GeoDistance.Meters(36.1, 138.8, 51.5, -114.4),
            precision: 6);
    }
}
