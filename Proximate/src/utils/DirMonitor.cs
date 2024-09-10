namespace Proximate;

public class DirMonitor : IDisposable
{
    private FileSystemWatcher watcher;
    private readonly string dirPath;
    private readonly Dictionary<string, DateTime> modTimes;
    public event Action<string>? OnUpdate;
    public event Action<string>? OnRemove;

    public DirMonitor(string dirPath)
    {
        if (!Directory.Exists(dirPath))
        {
            throw new ArgumentException("Directory does not exist.", nameof(dirPath));
        }

        this.dirPath = dirPath;
        modTimes = new Dictionary<string, DateTime>();

        watcher = new FileSystemWatcher(dirPath)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };

        watcher.Created += OnFileCreated;
        watcher.Changed += OnFileChanged;
        watcher.Deleted += OnFileDeleted;
        watcher.Renamed += OnFileRenamed;
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e) => HandleUpdate(e.FullPath);

    private void OnFileChanged(object sender, FileSystemEventArgs e) => HandleUpdate(e.FullPath);

    private void HandleUpdate(string filePath)
    {
        if (File.Exists(filePath))
        {
            DateTime lastWriteTime = File.GetLastWriteTime(filePath);
            if (!modTimes.TryGetValue(filePath, out DateTime previousWriteTime) || lastWriteTime > previousWriteTime)
            {
                modTimes[filePath] = lastWriteTime;
                OnUpdate?.Invoke(filePath);
            }
        }
    }

    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        if (modTimes.ContainsKey(e.FullPath))
        {
            modTimes.Remove(e.FullPath);
        }

        OnRemove?.Invoke(e.FullPath);
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        if (modTimes.ContainsKey(e.OldFullPath))
        {
            modTimes.Remove(e.OldFullPath);            
            OnRemove?.Invoke(e.FullPath);
        }

        if (File.Exists(e.FullPath))
        {
            modTimes[e.FullPath] = File.GetLastWriteTime(e.FullPath);
            OnUpdate?.Invoke(e.FullPath);
        }
    }

    public void Start()
    {
        watcher.EnableRaisingEvents = true;
        Console.WriteLine($"Monitoring directory: {dirPath}");
        var files = Directory.GetFiles(dirPath);
        foreach (var file in files)
        {
            HandleUpdate(file);
        }
    }

    public void Stop()
    {
        watcher.EnableRaisingEvents = false;
        Console.WriteLine($"Stopped monitoring directory: {dirPath}");
    }

    public void Dispose()
    {
        watcher.Dispose();
    }
}
