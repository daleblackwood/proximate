using Xunit;

namespace Proximate.Tests;

public class SnapshotLoggerTest
{
    [Fact]
    public void UnderstandsDaysAndNames()
    {
        Assert.Equal(1, SnapshotLogService.LocalDayStart(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Day);
        Assert.Equal(1, SnapshotLogService.LocalDayStart(new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc)).Day);
        Assert.Equal(2, SnapshotLogService.LocalDayStart(new DateTime(2025, 1, 1, 23, 0, 0, DateTimeKind.Utc)).Day);
        Assert.Equal(1, SnapshotLogService.LocalDayStart(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Local)).Day);
        Assert.Equal(1, SnapshotLogService.LocalDayStart(new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Local)).Day);
        Assert.Equal(1, SnapshotLogService.LocalDayStart(new DateTime(2025, 1, 1, 23, 0, 0, DateTimeKind.Local)).Day);

        Assert.Equal("2025-01-01", SnapshotLogService.GetDayLabel(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        Assert.Equal("2025-01-01", SnapshotLogService.GetDayLabel(new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc)));
        Assert.Equal("2025-01-02", SnapshotLogService.GetDayLabel(new DateTime(2025, 1, 1, 23, 0, 0, DateTimeKind.Utc)));
    }

    [Fact]
    public void StoreCorrectly()
    {
        var date = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var snapshotter = new Snapshotter();
        snapshotter.AddValue(date, "0", true);
        snapshotter.AddValue(date.AddHours(1), "1", false);
        snapshotter.AddValue(date.AddHours(2), "2", false);

        var logger = Services.Get<SnapshotLogService>();
        var entry = logger.Register("test", snapshotter);

        var outFile = logger.DumpSnapshot("test", entry, date, date.AddHours(1).AddTicks(1));
        Assert.True(string.IsNullOrEmpty(outFile) == false, "should have a good snapshot filename");
        Assert.True(File.Exists(outFile), "snapshot file should exist");

        // check deserialized values for second last line
        var expectedSnapshot = snapshotter.GetStoredSnapshot(snapshotter.GetStoredSnapshotCount() - 2)!;
        var expectedLine = expectedSnapshot.Serialize()!;
        var lastSnap = SnapshotLogReader.ReadLastSnapshot(outFile);
        Assert.Equal(expectedSnapshot.Time, lastSnap?.Time);
        Assert.Equal(expectedSnapshot.Value, lastSnap?.Value);
        Assert.Equal(expectedSnapshot.IsKey, lastSnap?.IsKey);

        // check deserialized string for second last line
        var lastLineStr = SnapshotLogReader.ReadLastLineText(outFile);
        Assert.Equal(expectedLine, lastLineStr);

        // dump again and check last line
        outFile = logger.DumpSnapshot("test", entry, date, date.AddYears(1000));
        Assert.True(string.IsNullOrEmpty(outFile) == false, "should have a good snapshot filename");
        expectedSnapshot = snapshotter.GetStoredSnapshot(snapshotter.GetStoredSnapshotCount() - 1)!;
        lastSnap = SnapshotLogReader.ReadLastSnapshot(outFile);
        Assert.Equal(expectedSnapshot.Time, lastSnap?.Time);
        Assert.Equal(expectedSnapshot.Value, lastSnap?.Value);
        Assert.Equal(expectedSnapshot.IsKey, lastSnap?.IsKey);
    }

}