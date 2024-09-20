using Swan;

namespace Proximate;

public class SnapshotLogService : Service
{

    public class SnapshotLogState 
    {
        public required string id;
        public required Snapshotter snapshotter;
        public DateTime lastTime = Timing.Epoch;
        public bool isWriting = false;
    }

    public static DateTime LocalDayStart(DateTime day)
    {
        var result = day;
        if (day.Kind != DateTimeKind.Local)
        {
            result = day.ToLocalTime();
        }
        return result.Date;
    }

    public static string GetDayLabel(DateTime day)
    {
        return LocalDayStart(day).ToString("yyyy-MM-dd");
    }

    public static string GetFilePath(string key, DateTime day)
    {
        var dayStr = GetDayLabel(day);
        var fileName = $"{key}-{dayStr}.log";
        var filePath = Env.ProjectPath("logs/" + fileName)!;
        return filePath;
    }

    public DateTime LastDump { get; private set; }
    public DateTime NextDump { get; private set; }

    private Dictionary<string, SnapshotLogState> snapshotters = new();
    private Timer timer;

    public SnapshotLogService() 
    { 
        var now = Timing.Now;
        timer = new Timer(
            OnTimer, 
            null, 
            (int)(Timing.Now.AddSeconds(10).RoundUp(TimeSpan.FromSeconds(10)) - Timing.Now).TotalMilliseconds, 
            (int)TimeSpan.FromMinutes(1).TotalMilliseconds
        );
        var pathAccess = GetFilePath("test", now);
        var dirName = Path.GetDirectoryName(pathAccess);
         if (dirName != null || Directory.Exists(dirName) == false)
        {
            Directory.CreateDirectory(dirName!);
        }
        if (Directory.Exists(dirName) == false)
        {
            throw new Exception("Directory cannot be written to.");
        }
        Log.Info("Logging to " + dirName);
    }

    public SnapshotLogState Register(string id, Snapshotter snapshotter)
    {
        if (snapshotters.ContainsKey(id))
        {
            throw new Exception($"Snapshotter {id} is already registered.");
        }
        var entry = new SnapshotLogState {
            id = id,
            snapshotter = snapshotter
        };
        snapshotters[id] = entry;
        return entry;
    }

    public DateTime GetLogTime(string id)
    {
        return snapshotters.ContainsKey(id) ? snapshotters[id].lastTime : Timing.Epoch;
    }
    
    private void OnTimer(object? state)
    {
        var start = LocalDayStart(Timing.Now);
        var end = start.AddDays(1);
        var snapshotKeys = snapshotters.Keys.ToArray();
        foreach (var key in snapshotKeys)
        {
            snapshotters.TryGetValue(key, out var snapshotter);
            if (snapshotter == null)
                continue;
            DumpIncrementalSnapshot(key, snapshotter, start, end);
        }
        LastDump = Timing.Now;
    }

    public string? DumpIncrementalSnapshot(string key, SnapshotLogState entry, DateTime start, DateTime end)
    {
        if (entry.lastTime > start)
        {
            start = entry.lastTime;
        }
        return DumpSnapshot(key, entry, start, end, true);
    }

    public string? DumpSnapshot(string key, SnapshotLogState entry, DateTime start, DateTime end, bool append = false)
    {
        if (entry.isWriting)
        {
            throw new FileLoadException("Snapshot is already dumping.");
        }

        try {
            entry.isWriting = true;
            var filePath = GetFilePath(key, start);
            if (filePath == null)
            {
                throw new Exception("File path is invalid");
            }
            
            var dirName = Path.GetDirectoryName(filePath);
            if (Directory.Exists(dirName) == false)
            {
                throw new Exception("Could not access log directory.");
            }

            var snapshotter = entry.snapshotter;
            var snapshotCount = snapshotter.GetStoredSnapshotCount();
            var startIndex = snapshotter.FindIndex(start);
            if (snapshotCount > 0 && startIndex < 0 && snapshotter.GetStoredSnapshot(0)!.Time < end)
            {
                startIndex = 0;
            }
            var storedCount = 0;
            if (startIndex >= 0)
            {
                using (var writer = new StreamWriter(filePath, append: append))
                {
                    for (var i = startIndex; i < snapshotCount; i++)
                    {
                        var snapshot = snapshotter.GetStoredSnapshot(i);
                        var line = snapshot?.Serialize();
                        if (string.IsNullOrEmpty(line) == false)
                        {
                            if (snapshot!.Time >= end)
                                break;
                            writer.WriteLine(line);
                            entry.lastTime = snapshot!.Time;
                            storedCount++;
                        }
                    }
                }
            }
            Log.Info($"Logged {storedCount} for {key}");
            
            var clearTime = start.AddDays(-2);
            snapshotter.ClearValuesBefore(clearTime);

            entry.isWriting = false;

            return filePath;
        }
        catch
        {
            entry.isWriting = false;
            throw;
        }
    }

}