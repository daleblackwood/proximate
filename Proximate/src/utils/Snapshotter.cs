namespace Proximate;

public class Snapshotter
{
    public class Snapshot
    {
        public DateTime Time { get; set;}
        public bool IsKey { get; set; } = true;
        public string Value { get; set; } = string.Empty;
        public int Length => Value?.Length ?? 0;

        public string? Serialize()
        {
            if (string.IsNullOrWhiteSpace(Value))
            {
                return null;
            }
            var line = Time.ToString("o");
            line += " " + (IsKey ? "K" : "D");
            line += " " + Compression.Compress(Value);
            return line;
        }

        public static Snapshot Deserialize(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                throw new ArgumentException("Input line cannot be null or empty.", nameof(line));
            }

            var parts = line.Split(' ', 3);
            if (parts.Length < 3)
            {
                throw new FormatException("Invalid snapshot format.");
            }

            if (!DateTime.TryParse(parts[0], null, System.Globalization.DateTimeStyles.AssumeUniversal, out var time))
            {
                throw new FormatException("Invalid timestamp format.");
            }
            if (time.Kind != DateTimeKind.Utc)
            {
                time = time.ToUniversalTime();
            }

            bool isKey = parts[1] == "K";
            string decompressedValue = Compression.Decompress(parts[2]);

            return new Snapshot {
                Time = time,
                IsKey = isKey,
                Value = decompressedValue
            };
        }
    }

    public class SnapshotResult
    {
        public readonly string Value;
        public readonly DateTime Time;

        public SnapshotResult(string value, DateTime time)
        {
            Value = value;
            Time = time;
        }

        public override string ToString()
        {
            return Value;
        }
    }

    private readonly List<Snapshot> snapshots = new();

    public DateTime LastUpdate => snapshots.Count > 0 ? snapshots[^1].Time : DateTime.UnixEpoch;

    public uint Count { get; private set; } = 0u;
    public uint KeyCount { get; private set; } = 0u;
    public ulong StorageSize { get; private set; } = 0u;
    
    public Snapshot? AddValue(DateTime time, string value, bool isKey)
    {
        if (snapshots.Count < 2)
        {
            isKey = true;
        }

        var prev = ReadValueAtIndex(snapshots.Count - 1);
        if (prev?.Value == value)
            return null;


        var snapshot = new Snapshot
        {
            Time = time,
            IsKey = isKey,
            Value = value
        };

        if (isKey == false)
        {
            try
            {
                snapshot.Value = StringDiff.Calculate(prev!.Value, value);
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to calculate diff: {ex.Message}");
                return null;
            }
        }

        snapshots.Add(snapshot);
        StorageSize += (ulong)snapshot.Length;
        Count++;
        if (isKey)
        {
            KeyCount++;
        }
        return snapshot;
    }

    public SnapshotResult? ReadValueAtTime(DateTime time)
    {
        int index = FindIndex(time);
        if (index < 0)
        {
            return null;
        }

        return ReadValueAtIndex(index);
    }

    public SnapshotResult? ReadValueAtIndex(int index)
    {
        var lastKeyIndex = -1;
        for (int i = index; i >= 0; i--)
        {
            if (snapshots[i].IsKey)
            {
                lastKeyIndex = i;
                break;
            }
        }
        if (lastKeyIndex < 0)
        {
            return null;
        }
        var result = snapshots[lastKeyIndex].Value;
        var resultTime = snapshots[lastKeyIndex].Time;
        if (lastKeyIndex == index)
        {
            return new SnapshotResult(result, resultTime);
        }

        for (int i = lastKeyIndex + 1; i <= index; i++)
        {
            var snapshot = snapshots[i];
            try
            {
                result = StringDiff.Apply(result, snapshot.Value);
                resultTime = snapshot.Time;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to apply diff: {ex.Message}");
                return null;
            }
            if (snapshot.IsKey)
            {
                break;
            }
        }
        return new SnapshotResult(result, resultTime);
    }

    public bool IsKey(int index)
    {
        if (index < 0 || index >= snapshots.Count)
        {
            return false;
        }
        return snapshots[index].IsKey;
    }

    public int FindIndex(DateTime time)
    {
        if (snapshots.Count > 0)
        {
            for (int i = snapshots.Count - 1; i >= 0; i--)
            {
                var snapshot = snapshots[i];
                if (snapshot.Time <= time)
                {
                    return i;
                }
            }
        }
        return -1;
    }

    public Snapshot? GetStoredSnapshot(int index)
    {
        if (index < 0 || index >= snapshots.Count)
            return null;

        return snapshots[index];
    }

    public int GetStoredSnapshotCount()
    {
        return snapshots.Count;
    }

    public Snapshot? GetLastSnapshot()
    {
        if (snapshots.Count == 0)
        {
            return null;
        }

        return snapshots[^1];
    }

    public void ClearValuesBefore(DateTime dateTime)
    {
        var index = FindIndex(dateTime);
        if (index < 1)
        {
            return;
        }
        snapshots.RemoveRange(0, index);
    }

    public Snapshot? GetLastKeySnapshot()
    {
        for (int i = snapshots.Count - 1; i >= 0; i--)
        {
            if (snapshots[i].IsKey)
            {
                return snapshots[i];
            }
        }
        return null;
    }
}
