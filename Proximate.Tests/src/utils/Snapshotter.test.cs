using Xunit;

namespace Proximate.Tests;

public class SnapshotterTest
{
    [Fact]
    public void IndexCorrectly()
    {
        var snapshotter = new Snapshotter();
        snapshotter.AddValue(DateTime.UnixEpoch, "0", true);
        snapshotter.AddValue(DateTime.UnixEpoch.AddHours(1), "1", true);
        snapshotter.AddValue(DateTime.UnixEpoch.AddHours(2), "2", true);

        Assert.True(snapshotter.Count == 3, "Should have 3 snapshots");
        Assert.True(snapshotter.IsKey(0), "Should 0 should be key");
        Assert.Equal(1, snapshotter.FindIndex(DateTime.UnixEpoch.AddHours(1)));
        Assert.Equal(2, snapshotter.FindIndex(DateTime.UnixEpoch.AddHours(2)));;

        Assert.Equal("0", snapshotter.ReadValueAtIndex(0)?.Value);
        Assert.Equal("1", snapshotter.ReadValueAtIndex(1)?.Value);
        Assert.Equal("2", snapshotter.ReadValueAtIndex(2)?.Value);

        Assert.Equal("0", snapshotter.ReadValueAtTime(DateTime.UnixEpoch)?.Value);
        Assert.Equal("0", snapshotter.ReadValueAtTime(DateTime.UnixEpoch.AddMinutes(1))?.Value);
        Assert.Equal("1", snapshotter.ReadValueAtTime(DateTime.UnixEpoch.AddHours(1))?.Value);
        Assert.Equal("1", snapshotter.ReadValueAtTime(DateTime.UnixEpoch.AddHours(1.1))?.Value);
        Assert.Equal("2", snapshotter.ReadValueAtTime(DateTime.UnixEpoch.AddHours(2))?.Value);
        Assert.Equal("2", snapshotter.ReadValueAtTime(DateTime.UnixEpoch.AddHours(24))?.Value);
    }

    [Fact]
    public void IndexCorrectlyAtSmallScale()
    {
        var snapshotter = new Snapshotter();
        snapshotter.AddValue(DateTime.UnixEpoch, "0", true);
        snapshotter.AddValue(DateTime.UnixEpoch.AddMilliseconds(1), "1", true);
        snapshotter.AddValue(DateTime.UnixEpoch.AddMilliseconds(2), "2", true);

        Assert.True(snapshotter.Count == 3, "Should have 3 snapshots");
        Assert.True(snapshotter.IsKey(0), "Should 0 should be key");
        Assert.Equal(1, snapshotter.FindIndex(DateTime.UnixEpoch.AddMilliseconds(1)));
        Assert.Equal(2, snapshotter.FindIndex(DateTime.UnixEpoch.AddMilliseconds(2)));
        Assert.Equal("1", snapshotter.ReadValueAtTime(DateTime.UnixEpoch.AddMilliseconds(1))?.Value);
        Assert.Equal("2", snapshotter.ReadValueAtTime(DateTime.UnixEpoch.AddMilliseconds(2))?.Value);
    }

    [Fact]
    public async Task RecordByTime()
    {
        var proxy = new MockTimeProxy();
        var snapshotter = new Snapshotter();
        var start = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        for (var i = 0; i < 10; i++)
        {
            var time = start.AddMilliseconds((i + 1) * 100);
            proxy.SetTime(time);
            var snapStr = await proxy.Update();
            snapshotter.AddValue(time, snapStr, i % 5 == 0);
        }

        Assert.True(snapshotter.Count == 10, "Should have 10 snapshots");
        Assert.True(snapshotter.IsKey(0), "Should 0 should be key");
        Assert.True(snapshotter.IsKey(5), "Should 5 should be key");
        Assert.False(snapshotter.IsKey(3), "Should 3 should not be key");
        Assert.False(snapshotter.IsKey(6), "Should 6 should not be key");

        var compareTime = start.AddMilliseconds(300);
        var compareStr = snapshotter.ReadValueAtTime(compareTime)?.Value;
        var compareData = Json.TryParse<MockTimeProxy.Response>(compareStr);
        Assert.True(compareData != null, "Should have valid state at 100ms");
        AssertUtils.IsTimeWithin(compareTime, compareData!.time, 1);

        await Task.Delay(100);

        compareTime = start.AddMilliseconds(900);
        compareStr = snapshotter.ReadValueAtTime(compareTime)?.Value;
        compareData = Json.TryParse<MockTimeProxy.Response>(compareStr);
        Assert.True(compareData != null, "Should have valid state at 900ms");
        AssertUtils.IsTimeWithin(compareTime, compareData!.time, 1);

        await Task.Delay(100);

        compareTime = start.AddMilliseconds(1000);
        compareStr = snapshotter.ReadValueAtTime(compareTime.AddHours(1))?.Value;
        compareData = Json.TryParse<MockTimeProxy.Response>(compareStr);
        Assert.True(compareData != null, "Should have valid state at past last state");
        AssertUtils.IsTimeWithin(compareTime, compareData!.time, 1);
    }

    [Fact]
    public void ClearsValuesBefore()
    {
        var snapshotter = new Snapshotter();
        snapshotter.AddValue(DateTime.UnixEpoch, "0", true);
        snapshotter.AddValue(DateTime.UnixEpoch.AddHours(1), "1", true);
        snapshotter.AddValue(DateTime.UnixEpoch.AddHours(2), "2", true);
        snapshotter.ClearValuesBefore(DateTime.UnixEpoch.AddHours(2));
        Assert.Equal(1, snapshotter.GetStoredSnapshotCount());
    }

}