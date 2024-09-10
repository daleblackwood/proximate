namespace Proximate;

public class SnapshotLogger
{
    public class SnapshotLogState 
    {
        public required string id;
        public required Snapshotter snapshotter;
        public DateTime lastTime = DateTime.UnixEpoch;
        public bool isWriting = false;
    }

    public static DateTime StartOfDay(DateTime day)
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
        return StartOfDay(day).ToString("yyyy-MM-dd");
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

    public SnapshotLogger() 
    { 
        var now = DateTime.UtcNow;
        var startOfHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);
        NextDump = startOfHour.AddHours(1);
        timer = new Timer(OnTimer, null, (int)(NextDump - DateTime.UtcNow).TotalMilliseconds, (int)TimeSpan.FromHours(1).TotalMilliseconds);
        var pathAccess = GetFilePath("test", startOfHour);
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
    
    private void OnTimer(object? state)
    {
        var start = StartOfDay(DateTime.UtcNow);
        var end = start.AddDays(1);
        foreach (var pair in snapshotters)
        {
            DumpSnapshot(pair.Key, pair.Value, start, end);
        }
        LastDump = DateTime.UtcNow;
    }

    public string? DumpSnapshot(string key, SnapshotLogState entry, DateTime start, DateTime end)
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
            var startIndex = snapshotter.FindIndex(start);
            var snapshotCount = snapshotter.GetStoredSnapshotCount();
            var storedCount = 0;
            if (startIndex >= 0)
            {
                using (var writer = new StreamWriter(filePath, append: false))
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