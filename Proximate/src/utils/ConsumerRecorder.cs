namespace Proximate;

public class ConsumerRecorder
{
    public bool IsRunning { get; private set; }
    public bool IsUpdating { get; private set; }

    public Snapshotter.Snapshot? LastSnapshot { get; private set; }

    public Snapshotter.Snapshot? LastKeySnapshot { get; private set; }

    public DateTime FirstUpdateTime { get; private set; } = Timing.Epoch;
    public DateTime LastUpdateTime { get; private set; }
    public DateTime NextUpdateTime => Timing.Greatest(Consumer.NextUpdateTime, LastUpdateTime.AddMilliseconds(tickIntervalMs));
    public DateTime LastSnapshotTime => LastSnapshot?.Time ?? Timing.Epoch;
    public DateTime LastKeySnapshotTime => LastKeySnapshot?.Time ?? Timing.Epoch;

    public string Id => Consumer.Id;

    public ulong MemorySize => Snapshotter.MemorySize;
    public ulong KeyshotCount => Snapshotter.KeyCount;
    public ulong SnapshotCount => Snapshotter.Count;
    
    public IConsumer Consumer { get; private set; }
    public Snapshotter Snapshotter { get; private set; } = new();
    private Timer? pollTimer = null;
    private uint snapshotIntervalMs;
    private uint tickIntervalMs;

    public ConsumerRecorder(IConsumer consumer, TimeSpan tickInterval, TimeSpan? snapsotInterval)
    {
        this.Consumer = consumer;
        this.tickIntervalMs = (uint)tickInterval.TotalMilliseconds;
        this.snapshotIntervalMs = (uint)(snapsotInterval ?? tickInterval.Multiply(10)).TotalMilliseconds;
    }

    public void SetConsumer(IConsumer consumer)
    {
        this.Consumer = consumer;
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
        if (!IsRunning) return;
        IsUpdating = true;

        var lt = Log.TimingBegin();
        var messages = await Consumer.Read();
        Log.TimingWarn(lt, 5, "Consumer Read: " + Consumer.Id);
        if (messages.Length > 0)
        {
            if (FirstUpdateTime == Timing.Epoch)
            {
                FirstUpdateTime = messages[0].time;
            }
            var lastKey = Snapshotter.GetLastKeySnapshot();
            foreach (var message in messages)
            {
                var isKey = lastKey == null || (message.time -  lastKey.Time).TotalMilliseconds >= snapshotIntervalMs;
                LastSnapshot = Snapshotter.AddValue(message.time, message.content, isKey);
                if (isKey) 
                {
                    lastKey = LastKeySnapshot;
                }
            }
            LastUpdateTime = messages[^1].time;
        }
        LastSnapshot = Snapshotter.GetLastSnapshot();
        LastKeySnapshot = Snapshotter.GetLastKeySnapshot();
        Log.Debug($"snapshots count: {Snapshotter.Count} last update: {Timing.Now}, diff size: {LastSnapshot?.Length ?? 0}, key size: {LastKeySnapshot?.Length ?? 0}");
        IsUpdating = false;
    }

    public Snapshotter.SnapshotResult? Read(DateTime time)
    {
        return Snapshotter.ReadValueAtTime(time);
    }

    public Snapshotter.SnapshotResult? Read(uint delayMs = 0)
    {
        return Read(Timing.Now.AddMilliseconds(-delayMs));
    }
}