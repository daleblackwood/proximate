namespace Proximate;

public class DirMonitor : IDisposable
{
    private readonly string dirPath;
    public event Action<string>? OnUpdate;
    public event Action<string>? OnRemove;
    public bool IsRunning { get; private set; }

    private Timer? timer;
    private readonly Dictionary<string, DateTime> modTimes = new();

    public DirMonitor(string dirPath)
    {
        if (!Directory.Exists(dirPath))
        {
            throw new ArgumentException("Directory does not exist.", nameof(dirPath));
        }
        this.dirPath = dirPath;
        Start();
    }

    public void Start()
    {
        if (IsRunning) return;
        Console.WriteLine($"Monitoring directory: {dirPath}");
        timer = new Timer(_ => Tick(), null, 0, 1000u);
        IsRunning = true;
    }

    public void Stop()
    {
        if (timer != null)
        {
            timer.Dispose();
            timer = null;
        }
        Console.WriteLine($"Stopped monitoring directory: {dirPath}");
        IsRunning = false;
    }

    private void Tick()
    {
        if (!IsRunning) return;
        var files = Directory.GetFiles(dirPath);
        foreach (var file in files)
        {
            var lastMod = File.GetLastWriteTime(file);
            if (modTimes.ContainsKey(file) == false || lastMod > modTimes[file])
            {
                OnUpdate?.Invoke(file);
                modTimes[file] = lastMod;
            }
        }

        foreach (var file in modTimes.Keys)
        {
            if (files.Contains(file) == false)
            {
                modTimes.Remove(file);
                OnRemove?.Invoke(file);
            }
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
