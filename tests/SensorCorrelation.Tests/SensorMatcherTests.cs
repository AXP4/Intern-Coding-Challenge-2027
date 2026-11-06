namespace SensorCorrelation.Tests;

// checking the matching rules with small made-up examples
public class SensorMatcherTests
{
    // 1 m of latitude in degrees (using the same Earth radius as GeoDistance)
    private const double OneMeter = 180 / (Math.PI * 6_371_008.8);

    // a reading placed a number of meters north of (0, 0), so distances are easy to set up
    private static SensorReading At(int id, double metersNorth) => new(id, metersNorth * OneMeter, 0);

    [Fact]
    public void SampleData_GivesTheSampleOutput()
    {
        var sensor1 = SensorDataReader.ReadCsv("Samples/SampleData1.csv").Readings;
        var sensor2 = SensorDataReader.ReadJson("Samples/SampleData2.json").Readings;

        var pairs = SensorMatcher.Match(sensor1, sensor2);

        Assert.Equal(new Dictionary<int, int> { [56] = 46, [24] = 74 }, pairs);
    }

    [Fact]
    public void ChallengeData_GivesElevenPairs()
    {
        var sensor1 = SensorDataReader.ReadCsv("SensorData1.csv").Readings;
        var sensor2 = SensorDataReader.ReadJson("SensorData2.json").Readings;

        var pairs = SensorMatcher.Match(sensor1, sensor2);

        var expected = new Dictionary<int, int>
        {
            [2] = 9, [6] = 19, [8] = 12, [11] = 37, [12] = 17, [15] = 23,
            [16] = 33, [17] = 5, [19] = 13, [21] = 29, [23] = 40,
        };
        Assert.Equal(expected, pairs);
    }

    [Fact]
    public void JustUnder200Meters_IsAMatch()
    {
        var pairs = SensorMatcher.Match([At(1, 0)], [At(2, 199.99)]);
        Assert.Equal(2, pairs[1]);
    }

    [Fact]
    public void JustOver200Meters_IsNotAMatch()
    {
        var pairs = SensorMatcher.Match([At(1, 0)], [At(2, 200.01)]);
        Assert.Empty(pairs);
    }

    [Fact]
    public void ClosestPartnerWins_WhenThereIsNoConflict()
    {
        // A has X at 150 m and Y at 20 m -> A takes Y
        var pairs = SensorMatcher.Match([At(1, 0)], [At(10, 150), At(20, -20)]);
        Assert.Equal(new Dictionary<int, int> { [1] = 20 }, pairs);
    }

    [Fact]
    public void TwoReadingsWantingOnePartner_CloserOneWins()
    {
        // A is 50 m from X, B is 30 m from X -> B gets X, A gets nothing
        var pairs = SensorMatcher.Match([At(1, -50), At(2, 30)], [At(10, 0)]);
        Assert.Equal(new Dictionary<int, int> { [2] = 10 }, pairs);
    }

    [Fact]
    public void AliAndAhmad_SomeoneMovesSoBothGetAPartner()
    {
        // Ali (1): car 10 at 100 m, car 20 at 150 m. Ahmad (2): only car 10, at 190 m.
        // Closest-first alone would give Ali car 10 and leave Ahmad with nothing.
        var ali = At(1, 0);
        var ahmad = At(2, 290);
        var car10 = At(10, 100);
        var car20 = At(20, -150);

        var pairs = SensorMatcher.Match([ali, ahmad], [car10, car20]);

        Assert.Equal(new Dictionary<int, int> { [1] = 20, [2] = 10 }, pairs);
    }

    [Fact]
    public void ABC_ChainOfAskingEnds_AndGivesTwoPairs()
    {
        // A can use X or Y, B only X, C only Y. Only 2 pairs are possible.
        // Without the visited list this would loop forever.
        var a = At(1, 150);
        var b = At(2, -100);
        var c = At(3, 400);
        var x = At(10, 0);
        var y = At(20, 300);

        var pairs = SensorMatcher.Match([a, b, c], [x, y]);

        Assert.Equal(new Dictionary<int, int> { [2] = 10, [3] = 20 }, pairs);
    }

    [Fact]
    public void Tie_PicksTheLowerId()
    {
        // like sensor 1 ID 21: sensor 2 IDs 29 and 39 are at the exact same spot
        var pairs = SensorMatcher.Match([At(21, 0)], [At(39, 139), At(29, 139)]);
        Assert.Equal(29, pairs[21]);
    }

    [Fact]
    public void SameIdInBothSensors_DoesNotMeanAMatch()
    {
        // sensor 1 ID 5 and sensor 2 ID 5 are far apart; sensor 2 ID 7 is the real partner
        var pairs = SensorMatcher.Match([At(5, 0)], [At(5, 5000), At(7, 50)]);
        Assert.Equal(new Dictionary<int, int> { [5] = 7 }, pairs);
    }

    [Fact]
    public void EmptyInput_GivesNoPairs()
    {
        Assert.Empty(SensorMatcher.Match([], [At(1, 0)]));
        Assert.Empty(SensorMatcher.Match([At(1, 0)], []));
    }
}
