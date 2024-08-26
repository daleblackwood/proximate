namespace Proximate;

public class Snapshotter
{
    public class Snapshot
    {
        public DateTime Time { get; set;}
        public bool IsKey { get; set; } = true;
        public string Value { get; set; } = string.Empty;
        public int Length => Value?.Length ?? 0;
    }

    public class SnapshotResult
    {
        public readonly string Value;
        public readonly DateTime Time;

        public SnapshotResult(string value, DateTime time)
        {
            this.Value = value;
            this.Time = time;
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
    
    public bool AddValue(DateTime time, string value, bool isKey)
    {
        if (snapshots.Count < 2)
        {
            isKey = true;
        }

        var prev = ReadValueAtIndex(snapshots.Count - 1);
        if (prev?.Value == value)
            return false;


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
                return false;
            }
        }

        snapshots.Add(snapshot);
        StorageSize += (ulong)snapshot.Length;
        Count++;
        if (isKey)
        {
            KeyCount++;
        }
        return true;
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

    public Snapshot? GetLastSnapshot()
    {
        if (snapshots.Count == 0)
        {
            return null;
        }

        return snapshots[^1];
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
