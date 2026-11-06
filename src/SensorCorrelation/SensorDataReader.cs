using System.Globalization;
using System.Text.Json;

namespace SensorCorrelation;

// The good readings from one file, plus a warning for every row that was skipped.
public record ReadResult(List<SensorReading> Readings, List<string> Warnings);

// Reads the sensor files. Bad rows are skipped with a warning, never fixed.
public static class SensorDataReader
{
    // reading the CSV file (sensor 1), one line at a time
    public static ReadResult ReadCsv(string path)
    {
        var rows = new List<(string Source, int? Id, double? Lat, double? Lon)>();
        int lineNumber = 0;

        foreach (var line in File.ReadLines(path))
        {
            lineNumber++;

            // skipping the header row and empty lines
            if (lineNumber == 1 || string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Split(',');
            string source = $"{Path.GetFileName(path)} line {lineNumber}";

            // a row without exactly 3 values is broken
            if (parts.Length != 3)
            {
                rows.Add((source, null, null, null));
                continue;
            }

            rows.Add((source, ParseInt(parts[0]), ParseDouble(parts[1]), ParseDouble(parts[2])));
        }

        return Validate(rows);
    }

    // reading the JSON file (sensor 2), one entry at a time
    public static ReadResult ReadJson(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));

        // the file has to be a list [ ... ] of readings
        if (document.RootElement.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException($"{Path.GetFileName(path)} should contain a list of readings [ ... ].");

        var rows = new List<(string Source, int? Id, double? Lat, double? Lon)>();
        int index = 0;

        foreach (var element in document.RootElement.EnumerateArray())
        {
            string source = $"{Path.GetFileName(path)} entry {index}";
            rows.Add((source, GetInt(element, "id"), GetDouble(element, "latitude"), GetDouble(element, "longitude")));
            index++;
        }

        return Validate(rows);
    }

    // checking every row from either file, keeping the good ones and warning about the bad ones
    private static ReadResult Validate(List<(string Source, int? Id, double? Lat, double? Lon)> rows)
    {
        var readings = new List<SensorReading>();
        var warnings = new List<string>();
        var seenIds = new HashSet<int>();

        foreach (var (source, id, lat, lon) in rows)
        {
            // null means the value was missing or not a number
            if (id is null || lat is null || lon is null)
            {
                warnings.Add($"{source}: missing or unreadable value, skipped.");
                continue;
            }

            var reading = new SensorReading(id.Value, lat.Value, lon.Value);

            // detecting coordinates out of bounds
            if (!reading.HasValidCoordinates())
            {
                warnings.Add($"{source}: ID {id} has out-of-range coordinates ({lat}, {lon}), skipped.");
                continue;
            }

            // detecting the same ID twice in one file (Add returns false if it was already there)
            if (!seenIds.Add(reading.Id))
            {
                warnings.Add($"{source}: ID {id} appears more than once in this file, skipped.");
                continue;
            }

            readings.Add(reading);
        }

        return new ReadResult(readings, warnings);
    }

    // turning CSV text into numbers, or null if it isn't a number
    // InvariantCulture means "51.5" is read the same on every computer (some expect "51,5")
    private static int? ParseInt(string text) =>
        int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;

    private static double? ParseDouble(string text) =>
        double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : null;

    // getting a number out of a JSON entry, or null if it's missing or not a number
    private static int? GetInt(JsonElement element, string name) =>
        element.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var value)
            ? value : null;

    private static double? GetDouble(JsonElement element, string name) =>
        element.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.Number && prop.TryGetDouble(out var value)
            ? value : null;
}
