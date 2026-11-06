namespace SensorCorrelation;

// Distance between two points on Earth, measured along the surface.
public static class GeoDistance
{
    // average radius of the Earth in meters (treating it as a sphere)
    private const double EarthRadiusMeters = 6_371_008.8;

    // haversine formula: great-circle distance in meters between two lat/long points
    public static double Meters(double lat1, double lon1, double lat2, double lon2)
    {
        // converting degrees to radians, because Math.Sin and Math.Cos use radians
        double lat1Rad = ToRadians(lat1);
        double lat2Rad = ToRadians(lat2);
        double deltaLat = ToRadians(lat2 - lat1);
        double deltaLon = ToRadians(lon2 - lon1);

        // combining the latitude and longitude differences
        // (cos of the latitudes shrinks the longitude difference, like in our estimate)
        double a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                   Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                   Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

        // turning that into the angle between the two points (seen from the Earth's center)
        double angle = 2 * Math.Asin(Math.Sqrt(a));

        // angle times radius = distance along the surface
        return EarthRadiusMeters * angle;
    }

    // distance between two sensor readings
    public static double Meters(SensorReading a, SensorReading b) =>
        Meters(a.Latitude, a.Longitude, b.Latitude, b.Longitude);

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}
