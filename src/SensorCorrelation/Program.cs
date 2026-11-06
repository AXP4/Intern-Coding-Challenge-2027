using System.Text.Json;
using SensorCorrelation;

// file paths can be given when running the program, otherwise the challenge files are used
string sensor1Path = args.Length > 0 ? args[0] : "SensorData1.csv";
string sensor2Path = args.Length > 1 ? args[1] : "SensorData2.json";

ReadResult sensor1, sensor2;
try
{
    sensor1 = SensorDataReader.ReadCsv(sensor1Path);
    sensor2 = SensorDataReader.ReadJson(sensor2Path);
}
// a missing or broken file stops the program with a short message instead of a crash
catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException)
{
    Console.Error.WriteLine($"ERROR: file not found: {e.Message}");
    return 1;
}
catch (Exception e) when (e is JsonException or InvalidDataException)
{
    Console.Error.WriteLine($"ERROR: could not read the JSON file: {e.Message}");
    return 1;
}

// warnings go to the error stream so they don't mix with the JSON output
foreach (var warning in sensor1.Warnings.Concat(sensor2.Warnings))
    Console.Error.WriteLine($"WARNING: {warning}");

var pairs = SensorMatcher.Match(sensor1.Readings, sensor2.Readings);

// printing the pairs as JSON, in the same format as SampleOutput.json, sorted by sensor 1 ID
var sorted = pairs.OrderBy(p => p.Key).ToDictionary(p => p.Key.ToString(), p => p.Value);
Console.WriteLine(JsonSerializer.Serialize(sorted, new JsonSerializerOptions { WriteIndented = true }));
return 0;
