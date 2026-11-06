# CUAVs-Coding-Challenge

Challenge Overview:

At Canadian UAVs, we handle large amounts of geospatial data, which is the focus of this challenge. The task involves correlating data from two sensors that detect anomalies. However, the sensors are not highly accurate, resulting in false positives and variations in their location readings. Your challenge is to associate the sensor readings based on their coordinates to identify common signals that may have been detected by both sensors. This correlation increases the likelihood that the signal is a genuine detection rather than a false positive.

Input Data:

The two sensors provide different output formats: one sensor outputs data in CSV format, and the other outputs data in JSON format. Please refer to the sample data for the exact format of each sensor's output. Both sensors assign a unique ID to each reading, but note that different sensors may use the same IDs. The sensor readings include location coordinates in decimal degrees, using the WGS 84 format, representing where the anomaly was detected. The sensors have an accuracy of 100 meters, meaning that the reported location is within 100 meters of the actual anomaly location.

Output:

The output should consist of pairs of IDs, where one ID is from the first sensor, and the second ID is from the second sensor.

---

# My Solution

A C# (.NET 10) program that finds readings from the two sensors that are probably the same anomaly.

## How to run

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download). Run these from the main folder of the repository.

```bash
dotnet run --project src/SensorCorrelation    # run on the challenge files
dotnet test                                   # run the tests
```

To run it on other files, add their paths:

```bash
dotnet run --project src/SensorCorrelation -- Samples/SampleData1.csv Samples/SampleData2.json
```

The output is in the same format as `SampleOutput.json`: sensor 1 ID → sensor 2 ID.

## Result

The sample files give exactly `SampleOutput.json`. The challenge files give 11 pairs:

```json
{ "2": 9, "6": 19, "8": 12, "11": 37, "12": 17, "15": 23,
  "16": 33, "17": 5, "19": 13, "21": 29, "23": 40 }
```

## How it works

1. Read both files and skip bad rows. A latitude must be between -90 and 90, and a longitude between -180 and 180. Each challenge file has 10 rows outside these limits. They are skipped and a warning is printed for each one.
2. Measure the distance between every sensor 1 reading and every sensor 2 reading, using the haversine formula (distance along the Earth's surface).
3. Keep pairs that are 200 m apart or less.
4. Match them up so that each reading is used only once and as many pairs as possible are found.

## Decisions

**Why 200 m?** In this document above, it was stated that each reading is within 100 m of the real anomaly. The farthest apart two readings can be is when they are on opposite sides of it: 100 + 100 = 200 m. "Within" includes exactly 100 m, so exactly 200 m counts as a match.

**Why skip bad rows instead of fixing them?** Some of them could be fixed in theory. Longitude 190° is the same line as -170°, because it just goes 10° past the 180° line, so it could be "wrapped" around. A latitude of 114 with a longitude of 51 could mean the two columns were swapped.

But a fix is only correct if you know what caused the error, and the data does not tell us that. Take sensor 1 ID 51, with longitude 182.456789. It could be a wrapped -177.54, a typo for 82.456789, or just garbage, and those are thousands of km apart. A latitude like 95 or 150 does not have an obvious fix at all.

Since there is no enough information to confirm any fix, I skipped these rows and printed a warning for each one. For a product used in aviation, a guessed location is worse than no location. I also didn't want to stop the whole program, because one bad row would throw away all the good data.

**Why not just pair each reading with its closest one?** Because it can leave readings out for no reason. Example:

- A is 100 m from X and 150 m from Y.
- B is 190 m from X, and too far from Y.

Closest first gives A-X, and B gets nothing. a better solution would be  A-Y and B-X, so we get 2 pairs instead of 1.

My program does this. Each reading tries its options from closest to farthest. If an option is taken, the reading holding it is asked to move to another option, and that one can ask someone else, and so on. A reading only moves if a free option is found at the end of the chain. Each option is only tried once per attempt, so this can't go in circles forever. (This is called maximum matching, or Kuhn's algorithm.)

**What about ties?** Sensor 2 IDs 29 and 39 are at the exact same spot, so sensor 1 ID 21 is equally close to both. The program picks the lower ID (29). I didn't use a random pick, because then the output could change every time it runs.

**Same IDs in both files.** Both sensors use some of the same IDs, like 43 and 44. They are different readings, so the program only compares locations, never IDs.

## Other options I considered

- **Hungarian algorithm:** it would also make the total distance of all pairs as small as possible. I looked into it, but I found it hard to understand well enough to explain every step or be sure my code was right. And I would rather submit something I fully understand and can test than something I cant explain. On this data it gives the same answer anyway, because no two readings compete for the same partner.
- **Vincenty's formula:** more exact than haversine, because the Earth is not a perfect sphere. The difference is about 1 m at 200 m, which is tiny next to the 100 m sensor error.
- **Faster searching:** the program compares every sensor 1 reading with every sensor 2 reading, which is O(n²). That's 1,600 checks here and takes under a millisecond, but with a million readings per sensor it would be a trillion. A grid would fix that: split the map into squares about 200 m wide and only compare readings in the same or neighbouring squares, since a match cant be farther away than that. That brings it close to O(n). I didn't build it, because for 40 readings it would add complexity to something that already works fine.

## Tests

There are 36 tests in `tests/SensorCorrelation.Tests`. They check:

- distances against known values
- the edges of the valid ranges (exactly 90 is valid, 90.0001 is not)
- files with missing values, text instead of numbers, and repeated IDs, plus missing or broken files (a short error message instead of a crash)
- the matching examples above: just under and just over 200 m, A and B wanting the same reading, ties, and a chain that has to give up
- that the sample files give `SampleOutput.json` and the challenge files give the 11 pairs above

## Files

```
src/SensorCorrelation/
  Program.cs            starts the program: read, match, print
  SensorReading.cs      one reading + the range check
  SensorDataReader.cs   reads the CSV and JSON files
  GeoDistance.cs        haversine distance
  SensorMatcher.cs      finds and matches the pairs
tests/SensorCorrelation.Tests/
```
