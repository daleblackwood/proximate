using System.Text;
using Xunit;

namespace Proximate.Tests;

public class LogReaderTest
{

    private string CreateTestFile(params string[] lines)
    {
        var tempFile = Path.GetTempFileName();
        File.WriteAllLines(tempFile, lines, Encoding.UTF8);
        return tempFile;
    }

    [Fact]
    public void FindLinePosition_ReturnsCorrectOffset()
    {
        var lines = new[]
        {
            "2025-04-01T00:00:00Z First line",
            "2025-04-02T00:00:00Z Second line",
            "2025-04-03T00:00:00Z Third line"
        };
        var file = CreateTestFile(lines);

        var targetTime = DateUtils.ParseIso("2025-04-02T00:00:00.000Z");
        var offset = LogReadUtils.FindLinePosition(file, targetTime, false);
        var (line, _) = LogReadUtils.ReadNextLine(file, offset);

        Assert.EndsWith("Second line", line);
        Assert.Equal(lines[1], line);

        (line, _) = LogReadUtils.ReadLine(file, DateUtils.ParseIso("2025-04-04T00:00:00Z"), false);
        Assert.Equal(lines[2], line);

        (line, _) = LogReadUtils.ReadLine(file, DateUtils.ParseIso("2025-04-02T12:00:00Z"), false);
        Assert.Equal(lines[1], line);

        File.Delete(file);
    }

    [Fact]
    public void ReadNextLine_ReadsLineCorrectly()
    {
        var lines = new[]
        {
            "2025-04-01T00:00:00Z First",
            "2025-04-02T00:00:00Z Second",
        };
        var file = CreateTestFile(lines);

        long pos = 0;
        var (line1, _) = LogReadUtils.ReadNextLine(file, pos);
        Assert.Equal(lines[0], line1);

        if (string.IsNullOrEmpty(line1))
        {
            var (line2, _) = LogReadUtils.ReadNextLine(file, line1!.Length + Environment.NewLine.Length);
            Assert.Equal(lines[1], line2);
        }

        File.Delete(file);
    }

    [Fact]
    public void ReadPreviousLine_ReadsPreviousCorrectly()
    {
        var lines = new[]
        {
            "2025-04-01T00:00:00Z First",
            "2025-04-02T00:00:00Z Second",
            "2025-04-03T00:00:00Z Third"
        };

        var file = CreateTestFile(lines);
        var pos = new FileInfo(file).Length - 1;
        var prev = LogReadUtils.ReadPreviousLine(file, pos);
        Assert.Equal(lines[2], prev.line);

        pos -= Encoding.UTF8.GetByteCount(lines[2] + Environment.NewLine);
        var second = LogReadUtils.ReadPreviousLine(file, pos);

        Assert.Equal(lines[1], second.line);

        File.Delete(file);
    }

    [Fact]
    public void ReadLastLine_ReturnsLastLine()
    {
        var lines = new[]
        {
            "2025-04-01T00:00:00Z Start",
            "2025-04-02T00:00:00Z Middle",
            "2025-04-03T00:00:00Z End"
        };
        var file = CreateTestFile(lines);

        var (last, _) = LogReadUtils.ReadLastLine(file);
        Assert.Equal(lines[2], last);

        File.Delete(file);
    }

    [Fact]
    public void ReadLinesSequentially()
    {
        var lines = new[]
        {
            "2025-04-01T00:00:00Z 1",
            "2025-04-02T00:00:00Z 2",
            "2025-04-03T00:00:00Z 3",
            "2025-04-03T00:00:00Z 4",
            "2025-04-03T00:00:00Z 5",
            "2025-04-03T00:00:00Z 6"
        };
        var file = CreateTestFile(lines);

        string? line;
        long position = 0;
        (line, position) = LogReadUtils.ReadNextLine(file, position);
        Assert.Equal(lines[0], line);
        (line, position) = LogReadUtils.ReadNextLine(file, position);
        Assert.Equal(lines[1], line);
        (line, position) = LogReadUtils.ReadNextLine(file, position);
        Assert.Equal(lines[2], line);
        (line, position) = LogReadUtils.ReadNextLine(file, position);
        Assert.Equal(lines[3], line);
        (line, position) = LogReadUtils.ReadNextLine(file, position);
        Assert.Equal(lines[4], line);
        (line, _) = LogReadUtils.ReadNextLine(file, position);
        Assert.Equal(lines[5], line);

        File.Delete(file);
    }

    [Fact]
    public void ReadLinesInReverse()
    {
        var lines = new[]
        {
            "2025-04-01T00:00:00Z 0",
            "2025-04-02T00:00:00Z 1",
            "2025-04-03T00:00:00Z 2",
            "2025-04-03T00:00:00Z 3",
            "2025-04-03T00:00:00Z 4",
            "2025-04-03T00:00:00Z 5"
        };
        var file = CreateTestFile(lines);

        string? line;
        long position;
        (line, position) = LogReadUtils.ReadLastLine(file);
        Assert.Equal(lines[5], line);
        (line, position) = LogReadUtils.ReadPreviousLine(file, position);
        Assert.Equal(lines[4], line);
        (line, position) = LogReadUtils.ReadPreviousLine(file, position);
        Assert.Equal(lines[3], line);
        (line, position) = LogReadUtils.ReadPreviousLine(file, position);
        Assert.Equal(lines[2], line);
        (line, position) = LogReadUtils.ReadPreviousLine(file, position);
        Assert.Equal(lines[1], line);
        (line, _) = LogReadUtils.ReadPreviousLine(file, position);
        Assert.Equal(lines[0], line);

        File.Delete(file);
    }
}