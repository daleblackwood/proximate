namespace Proximate;

public class ProxyRecorder
{
    public bool IsRunning { get; private set; }
    public bool IsUpdating { get; private set; }
    
    private readonly IProxy proxy;
    private Snapshotter snapshotter = new();
    private Timer? pollTimer = null;
    private uint snapshotIntervalMs = 120000;
    private uint tickIntervalMs = 5000;

    public ProxyRecorder(IProxy proxy)
    {
        this.proxy = proxy;
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
            var isKey = last == null || (DateTime.UtcNow - last.Time).TotalMilliseconds > snapshotIntervalMs;
            snapshotter.AddValue(DateTime.UtcNow, content, isKey);
        }

        var lastSnap = snapshotter.GetLastSnapshot();
        var lastKey = snapshotter.GetLastKeySnapshot();
        Console.WriteLine($"snapshots count: {snapshotter.Count} last update: {DateTime.UtcNow}, diff size: {lastSnap?.Length ?? 0}, key size: {lastKey?.Length ?? 0}");

        IsUpdating = false;
    }
}