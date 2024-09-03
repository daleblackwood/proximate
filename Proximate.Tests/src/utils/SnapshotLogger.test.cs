using Xunit;

namespace Proximate.Tests;

public class SnapshotLoggerTest
{
    [Fact]
    public void UnderstandsDaysAndNames()
    {
        Assert.Equal(1, SnapshotLogger.StartOfDay(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Day);
        Assert.Equal(1, SnapshotLogger.StartOfDay(new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc)).Day);
        Assert.Equal(2, SnapshotLogger.StartOfDay(new DateTime(2025, 1, 1, 23, 0, 0, DateTimeKind.Utc)).Day);
        Assert.Equal(1, SnapshotLogger.StartOfDay(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Local)).Day);
        Assert.Equal(1, SnapshotLogger.StartOfDay(new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Local)).Day);
        Assert.Equal(1, SnapshotLogger.StartOfDay(new DateTime(2025, 1, 1, 23, 0, 0, DateTimeKind.Local)).Day);

        Assert.Equal("2025-01-01", SnapshotLogger.GetDayLabel(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        Assert.Equal("2025-01-01", SnapshotLogger.GetDayLabel(new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc)));
        Assert.Equal("2025-01-02", SnapshotLogger.GetDayLabel(new DateTime(2025, 1, 1, 23, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public void StoreCorrectly()
    {
        var snapshotter = new Snapshotter();
        snapshotter.AddValue(DateTime.UnixEpoch, "0", true);
        snapshotter.AddValue(DateTime.UnixEpoch.AddHours(1), "1", true);
        snapshotter.AddValue(DateTime.UnixEpoch.AddHours(2), "2", true);

        var logger = new SnapshotLogger();
        logger.Register("test", snapshotter);

        var outFile = logger.DumpSnapshot("test", snapshotter, DateTime.UnixEpoch, DateTime.UnixEpoch.AddDays(1));
        Assert.True(string.IsNullOrEmpty(outFile) == false, "should have a good snapshot filename");
        Assert.True(File.Exists(outFile), "snapshot file should exist");
    }

}