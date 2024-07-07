namespace Proximate;

public class Snapshotter
{
    public class Snapshot
    {
        public DateTime Time { get; set; } = DateTime.UnixEpoch;
        public bool IsKey { get; set; } = true;
        public string Value { get; set; } = string.Empty;
        public int Length => Value?.Length ?? 0;
    }

    private readonly List<Snapshot> snapshots = new();

    public int Count => snapshots.Count;
    public DateTime LastUpdate => snapshots.Count > 0 ? snapshots[^1].Time : DateTime.UnixEpoch;

    public bool AddValue(DateTime time, string value, bool isKey)
    {
        if (snapshots.Count < 2)
        {
            isKey = true;
        }

        var prev = ReadValueAtIndex(snapshots.Count - 1);
        if (prev == value)
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
                snapshot.Value = StringDiff.Calculate(prev, value);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to calculate diff: {ex.Message}");
                return false;
            }
        }

        snapshots.Add(snapshot);
        return true;
    }

    public string? ReadValueAtTime(DateTime time)
    {
        int index = FindIndex(time);
        if (index < 0)
        {
            return null;
        }

        return ReadValueAtIndex(index);
    }

    public string ReadValueAtIndex(int index)
    {
        string result = string.Empty;
        for (int i = index; i >= 0; i--)
        {
            var snapshot = snapshots[i];
            try
            {
                result = StringDiff.Apply(snapshot.Value, result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to apply diff: {ex.Message}");
                return string.Empty;
            }
            if (snapshot.IsKey)
            {
                break;
            }
        }

        return result;
    }

    public int FindIndex(DateTime time)
    {
        for (int i = 0; i < snapshots.Count; i++)
        {
            if (snapshots[i].Time > time)
            {
                return i;
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
