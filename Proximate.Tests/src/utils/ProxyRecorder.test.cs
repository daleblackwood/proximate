using Proximate;
using Xunit;

namespace Proximate.Tests;

public class ProxyRecorderTest
{
    [Fact]
    public async Task ProgressivelyUpdate()
    {
        var proxy = new MockTimeProxy();
        var intervalMs = 100u;
        var snapshotMs = 1000u;
        var recorder = new ProxyRecorder(proxy, intervalMs, snapshotMs);

        var start = DateTime.UtcNow;
        recorder.Start();

        await Task.Delay(10);

        for (var i = 0; i < 10; i++)
        {
            await Task.Delay((int)intervalMs);

            var snap = Json.TryParse<MockTimeProxy.Response>(recorder.Read()!);
            Assert.True(snap != null, "Should have valid state");
            //AssertUtil.IsTimeWithin(snap.time, start + TimeSpan.FromMilliseconds((i + 1) * intervalMs), (int)intervalMs, $"Snapshot {i} should be at about {i * intervalMs}ms");
            AssertUtil.IsTimeWithin(snap.time, start + TimeSpan.FromMilliseconds((i + 1) * intervalMs), 500, $"Snapshot {i} should be at about {i * intervalMs}ms");
        }
    }

    [Fact]
    public async Task CrawlBackInTime()
    {
        var proxy = new MockTimeProxy();
        var intervalMs = 100u;
        var snapshotMs = 1000u;
        var recorder = new ProxyRecorder(proxy, intervalMs, snapshotMs);

        var start = DateTime.UtcNow;
        recorder.Start();
        var firstDelay = (int)snapshotMs + 10;
        await Task.Delay(firstDelay);
        
        var snap = Json.TryParse<MockTimeProxy.Response>(recorder.Read()!);
        Assert.True(snap != null, "Should have valid state");
        AssertUtil.IsTimeWithin(snap.time, start.AddMilliseconds(firstDelay), 50, "Should correct time.");
    }
}