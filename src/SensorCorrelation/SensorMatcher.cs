namespace SensorCorrelation;

// Pairs sensor 1 readings with sensor 2 readings using maximum matching (augmenting paths).
// Each reading is used in at most one pair, and we get as many pairs as possible.
public static class SensorMatcher
{
    // two readings can be the same anomaly if they are 200 m apart or less (100 m error on each sensor)
    public const double MaxDistanceMeters = 200;

    // returns sensor 1 ID -> sensor 2 ID
    public static Dictionary<int, int> Match(List<SensorReading> sensor1, List<SensorReading> sensor2)
    {
        // for every sensor 1 reading: the sensor 2 readings within 200 m, closest first (ties: lower ID)
        var options = new Dictionary<SensorReading, List<SensorReading>>();
        foreach (var a in sensor1)
        {
            options[a] = sensor2
                .Where(b => GeoDistance.Meters(a, b) <= MaxDistanceMeters)
                .OrderBy(b => GeoDistance.Meters(a, b))
                .ThenBy(b => b.Id)
                .ToList();
        }

        // turn order: readings with the closest partner go first (ties: lower ID)
        var turnOrder = sensor1
            .Where(a => options[a].Count > 0)
            .OrderBy(a => GeoDistance.Meters(a, options[a][0]))
            .ThenBy(a => a.Id)
            .ToList();

        // who currently holds each sensor 2 reading
        var holderOf = new Dictionary<SensorReading, SensorReading>();

        foreach (var a in turnOrder)
        {
            // a fresh visited list for every new attempt, so we never go in circles
            var visited = new HashSet<SensorReading>();
            TryToMatch(a, options, holderOf, visited);
        }

        // flipping holderOf around into the output format: sensor 1 ID -> sensor 2 ID
        return holderOf.ToDictionary(pair => pair.Value.Id, pair => pair.Key.Id);
    }

    // tries to find a partner for reading a; returns true if it worked
    // if the partner is taken, asks the holder to move (this function calls itself to do that)
    private static bool TryToMatch(
        SensorReading a,
        Dictionary<SensorReading, List<SensorReading>> options,
        Dictionary<SensorReading, SensorReading> holderOf,
        HashSet<SensorReading> visited)
    {
        foreach (var b in options[a])
        {
            // skipping readings we already asked about in this attempt
            if (!visited.Add(b))
                continue;

            // b is free, or b's holder managed to move somewhere else -> a takes b
            if (!holderOf.ContainsKey(b) || TryToMatch(holderOf[b], options, holderOf, visited))
            {
                holderOf[b] = a;
                return true;
            }
        }

        // no option worked, so nothing changes
        return false;
    }
}
