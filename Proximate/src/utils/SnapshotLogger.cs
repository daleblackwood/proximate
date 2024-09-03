namespace Proximate;

public class SnapshotLogger
{
    private const string LOG_DIR = "logs"; 

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
        var logDir = Env.ProjectPath(LOG_DIR);
        var filePath = Path.Join(logDir, fileName)!;
        return filePath;
    }

    public DateTime LastDump { get; private set; }
    public DateTime NextDump { get; private set; }

    private Dictionary<string, Snapshotter> snapshotters = new();
    private Timer timer;

    public SnapshotLogger() 
    { 
        var now = DateTime.UtcNow;
        var startOfHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);
        NextDump = startOfHour.AddHours(1);
        timer = new Timer(OnTimer, null, (int)(NextDump - DateTime.UtcNow).TotalMilliseconds, (int)TimeSpan.FromHours(1).TotalMilliseconds);
    }

    public void Register(string id, Snapshotter snapshotter)
    {
        if (snapshotters.ContainsKey(id))
        {
            throw new Exception($"Snapshotter {id} is already registered.");
        }
        snapshotters[id] = snapshotter;
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

    public string? DumpSnapshot(string key, Snapshotter snapshotter, DateTime start, DateTime end)
    {
        var filePath = GetFilePath(key, start);
        if (filePath == null)
        {
            throw new Exception("File path is invalid");
        }
        var startIndex = snapshotter.FindIndex(start);
        if (startIndex < 0)
        {
            Log.Info($"Nothing for log for {key}");
            return null;
        }
        var endIndex = snapshotter.FindIndex(end);
        if (endIndex < 0)
        {
            endIndex = snapshotter.GetStoredSnapshotCount();
        }
        
        var dirName = Path.GetDirectoryName(filePath);
        if (dirName != null || Directory.Exists(dirName) == false)
        {
            Directory.CreateDirectory(dirName!);
        }
        if (Directory.Exists(dirName) == false)
        {
            throw new Exception("Could not access log directory.");
        }

        using (var writer = new StreamWriter(filePath, append: false))
        {
            for (var i = startIndex; i < endIndex; i++)
            {
                var snapshot = snapshotter.GetStoredSnapshot(i);
                var line = snapshot?.Serialize();
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }
                writer.WriteLine(line);
            }
        }

        var clearTime = start.AddDays(-2);
        snapshotter.ClearValuesBefore(clearTime);

        return filePath;
    }

}