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
        var recorder = new ConsumerRecorder(proxy, intervalMs, snapshotMs);

        var start = Timing.Now;
        recorder.Start();

        await Task.Delay(20);

        for (var i = 0; i < 10; i++)
        {
            var snap = Json.TryParse<MockTimeProxy.Response>(recorder.Read()?.Value!);
            var targetTime = start.AddMilliseconds(i * intervalMs + 20);
            Assert.True(snap != null, "Should have valid state");
            AssertUtils.IsTimeWithin(snap.time, targetTime, 40, $"Bad time {i}");
            await Task.Delay((int)intervalMs);
        }
    }

    [Fact]
    public async Task CrawlBackInTime()
    {
        var proxy = new MockTimeProxy();
        var intervalMs = 100u;
        var snapshotMs = 1000u;
        var recorder = new ConsumerRecorder(proxy, intervalMs, snapshotMs);

        var start = Timing.Now;
        recorder.Start();

        var bufferMs = 200u;
        var waitMs = 510;
        await Task.Delay(waitMs);

        var snap = Json.TryParse<MockTimeProxy.Response>(recorder.Read(bufferMs)?.Value);
        Assert.True(snap != null, "Should have valid state 2");
        AssertUtils.IsTimeWithin(snap.time, start.AddMilliseconds(waitMs - bufferMs), 40, "Should correct time.");
    }
}