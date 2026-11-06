namespace SensorCorrelation.Tests;

// reading files with bad rows: the good rows are kept, every bad row gives a warning
public class SensorDataReaderTests : IDisposable
{
    private readonly string _folder = Directory.CreateTempSubdirectory("sensor-tests").FullName;

    // deleting the temporary files after each test
    public void Dispose() => Directory.Delete(_folder, recursive: true);

    private string WriteFile(string name, string content)
    {
        string path = Path.Combine(_folder, name);
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public void Csv_KeepsGoodRows_AndWarnsAboutEachBadRow()
    {
        string path = WriteFile("s1.csv", """
            id,latitude,longitude
            1,51.5,-114.4
            2,95.0,-114.4
            3,51.5,-190
            4,abc,-114.4
            5,51.5

            1,40.0,40.0
            6,-90,180
            """);

        var result = SensorDataReader.ReadCsv(path);

        Assert.Equal(new[] { 1, 6 }, result.Readings.Select(r => r.Id));
        Assert.Equal(5, result.Warnings.Count); // rows 2, 3, 4, 5 and the second ID 1 (blank line is ignored)
    }

    [Fact]
    public void Csv_ReadsTheRealValues()
    {
        string path = WriteFile("s1.csv", "id,latitude,longitude\n8,50.96850558,-114.3583294\n");

        var reading = Assert.Single(SensorDataReader.ReadCsv(path).Readings);

        Assert.Equal(new SensorReading(8, 50.96850558, -114.3583294), reading);
    }

    [Fact]
    public void Json_KeepsGoodEntries_AndWarnsAboutEachBadEntry()
    {
        string path = WriteFile("s2.json", """
            [
              { "id": 1, "latitude": 51.5, "longitude": -114.4 },
              { "id": 2, "latitude": 150.2, "longitude": 190.3 },
              { "id": 3, "latitude": 51.5 },
              { "id": 4, "latitude": "51.5", "longitude": -114.4 },
              { "id": 1, "latitude": 10, "longitude": 10 },
              { "id": 5, "latitude": 0, "longitude": 0 }
            ]
            """);

        var result = SensorDataReader.ReadJson(path);

        Assert.Equal(new[] { 1, 5 }, result.Readings.Select(r => r.Id));
        Assert.Equal(4, result.Warnings.Count); // out of range, missing longitude, text instead of number, repeated ID
    }

    [Fact]
    public void MissingFile_GivesFileNotFound()
    {
        Assert.Throws<FileNotFoundException>(() => SensorDataReader.ReadCsv(Path.Combine(_folder, "nope.csv")));
    }

    [Fact]
    public void BrokenJson_GivesJsonError()
    {
        string path = WriteFile("broken.json", """[ { "id": 1, "latitude": """);
        Assert.ThrowsAny<System.Text.Json.JsonException>(() => SensorDataReader.ReadJson(path));
    }

    [Fact]
    public void JsonThatIsNotAList_GivesClearError()
    {
        string path = WriteFile("object.json", """{ "id": 1, "latitude": 0, "longitude": 0 }""");
        Assert.Throws<InvalidDataException>(() => SensorDataReader.ReadJson(path));
    }

    [Fact]
    public void ChallengeFiles_SkipTheTenBadRowsInEachFile()
    {
        var sensor1 = SensorDataReader.ReadCsv("SensorData1.csv");
        var sensor2 = SensorDataReader.ReadJson("SensorData2.json");

        Assert.Equal(40, sensor1.Readings.Count);
        Assert.Equal(10, sensor1.Warnings.Count);
        Assert.Equal(40, sensor2.Readings.Count);
        Assert.Equal(10, sensor2.Warnings.Count);
    }
}
