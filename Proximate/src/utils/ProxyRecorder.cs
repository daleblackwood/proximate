namespace Proximate;

public class ProxyRecorder
{
    public bool IsRunning { get; private set; }
    public bool IsUpdating { get; private set; }

    public Snapshotter.SnapshotResult? FirstSnapshot => snapshotter.ReadValueAtIndex(0);

    public Snapshotter.Snapshot? LastSnapshot { get; private set; }
    public Snapshotter.Snapshot? LastKeySnapshot { get; private set; }

    public DateTime LastUpdateTime => proxy.LastUpdateTime;

    public string Id => proxy.Id;

    public ulong StorageSize => snapshotter.StorageSize;
    public ulong KeyshotCount => snapshotter.KeyCount;
    public ulong SnapshotCount => snapshotter.Count;
    
    private readonly IProxy proxy;
    private Snapshotter snapshotter = new();
    private Timer? pollTimer = null;
    private uint snapshotIntervalMs;
    private uint tickIntervalMs;

    public ProxyRecorder(IProxy proxy, uint tickIntervalMs = 1000, uint snapshotIntervalMs = 120000)
    {
        this.proxy = proxy;
        this.tickIntervalMs = tickIntervalMs;
        this.snapshotIntervalMs = snapshotIntervalMs;
    }

    public void Start()
    {
        Stop();
        IsRunning = true;
        pollTimer = new Timer(_ => OnTick(), null, 0, tickIntervalMs);
    }

    public void Stop()
    {
        if (pollTimer != null)
        {
            pollTimer.Dispose();
            pollTimer = null;
        }
        IsRunning = false;
    }

    private void OnTick()
    {
        if (IsUpdating)
            return;

        _ = Update();
    }

    private async Task Update()
    {
        IsUpdating = true;

        var content = await proxy.Update();
        if (IsRunning)
        {
            var last = snapshotter.GetLastKeySnapshot();
            var lastDuration = last == null ? snapshotIntervalMs : (DateTime.UtcNow - last.Time).TotalMilliseconds;
            var isKey = lastDuration >= snapshotIntervalMs;
            snapshotter.AddValue(DateTime.UtcNow, content, isKey);
        }

        LastSnapshot = snapshotter.GetLastSnapshot();
        LastKeySnapshot = snapshotter.GetLastKeySnapshot();
        Log.Info($"snapshots count: {snapshotter.Count} last update: {DateTime.UtcNow}, diff size: {LastSnapshot?.Length ?? 0}, key size: {LastKeySnapshot?.Length ?? 0}");

        IsUpdating = false;
    }

    public Snapshotter.SnapshotResult? Read(DateTime time)
    {
        return snapshotter.ReadValueAtTime(time);
    }

    public Snapshotter.SnapshotResult? Read(uint delayMs = 0)
    {
        return Read(DateTime.UtcNow.AddMilliseconds(-delayMs));
    }
}