namespace SensorCorrelation;

// One reading from a sensor: its ID and the location where it detected an anomaly.
public record SensorReading(int Id, double Latitude, double Longitude)
{
    // detecting coordinates out of bounds
    public bool HasValidCoordinates() =>
        Latitude >= -90 && Latitude <= 90 &&
        Longitude >= -180 && Longitude <= 180;
}
