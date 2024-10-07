using System.Globalization;
using System.Text;

namespace Proximate;

public static class LogReadUtils
{
    private const int BUFFER_SIZE = 1024;

    public static long FindLinePosition(string filename, DateTime targetTime, bool keyRequired)
    {
        targetTime = targetTime.AddMilliseconds(1);
        filename = AssertFilename(filename);
        int narrowingSize = 100 * BUFFER_SIZE;
        using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            long fileSize = fs.Length;
            long low = 0;
            long high = fileSize;
            var (firstLine, firstLinePos) = ReadLineAt(fs, 0);
            if (firstLine != null)
            {
                DateTime firstTs = ParseTimestamp(firstLine);
                // early return if first Date is beyond the target time
                if (firstTs > targetTime)
                    return 0;
            }
            // bsearch narrowing of log lines to find date
            while (high - low > narrowingSize)
            {
                long mid = low + (high - low) / 2;
                var (line, linePos) = ReadLineAt(fs, mid);
                if (line == null)
                    break;
                DateTime ts;
                try
                {
                    ts = ParseTimestamp(line);
                }
                catch (FormatException)
                {
                    low = fs.Position;
                    continue;
                }
                if (ts <= targetTime)
                {
                    low = fs.Position;
                }
                else
                {
                    high = linePos;
                }
            }
            long result = -1;
            // search through each chunk backwards by the narrowing size until a keyframe is found
            while (result < 0)
            {
                low = Math.Max(0, low - narrowingSize);
                fs.Seek(low, SeekOrigin.Begin);
                int regionSize = (int)(high - low);
                byte[] buffer = new byte[regionSize];
                if (fs.Read(buffer, 0, regionSize) == 0)
                {
                    return -1;
                }
                var regionText = Encoding.UTF8.GetString(buffer);
                using (var sr = new StringReader(regionText))
                {
                    long currentOffset = low;
                    string? line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        DateTime ts;
                        try
                        {
                            ts = ParseTimestamp(line);
                        }
                        catch (FormatException)
                        {
                            currentOffset += Encoding.UTF8.GetByteCount(line) + Encoding.UTF8.GetByteCount(Environment.NewLine);
                            continue;
                        }
                        if (ts <= targetTime && (!keyRequired || IsLineKey(line)))
                        {
                            result = currentOffset; // it's beforehand and it's a keyframe (or it doesn't matter)
                        }
                        else if (ts > targetTime)
                        {
                            break; // it's after, so we'll read another buffer
                        }
                        currentOffset += Encoding.UTF8.GetByteCount(line) + Encoding.UTF8.GetByteCount(Environment.NewLine);
                    }
                }
                low -= narrowingSize; // go to the previous buffer
                if (low < 0)
                    break;
            }
            return result;
        }
    }

    public static (string? line, long position) ReadLine(string filename, DateTime targetTime, bool keyRequired)
    {
        var nextPosition = FindLinePosition(filename, targetTime, keyRequired);
        if (nextPosition >= 0)
        {
            return ReadNextLine(filename, nextPosition);;
        }
        return (null, -1);
    }

    public static (string? line, long position) ReadNextLine(string filename, long position)
    {
        if (position < 0)
        {
            throw new Exception("Invalid position.");
        }
        filename = AssertFilename(filename);
        using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            return ReadNextLine(fs, position);
        }
    }

    public static (string? line, long position) ReadNextLine(FileStream fs, long position)
    {
        using (var sr = new StreamReader(fs, Encoding.UTF8, true, BUFFER_SIZE))
        {
            fs.Seek(position, SeekOrigin.Begin);
            string? line = null;
            if (position != 0)
            {
                line = sr.ReadLine();
            }
            if (line != null)
            {
                position += line.Length;
                try 
                {
                    // will return the current line if the position was at the beginning of the line
                    ParseTimestamp(line);
                    return (line, position + line.Length);
                } catch {}
            }
            // returns the next line if position was part way
            line = sr.ReadLine();
            return (CleanLine(line), position + (line?.Length ?? 0));
        }
    }
    
    private static long GetPreviousLinePosition(FileStream stream, long fromPosition)
    {
        if (fromPosition <= 0) return -1;

        var buffer = new byte[BUFFER_SIZE];

        while (fromPosition > 0)
        {
            long start = Math.Max(fromPosition - BUFFER_SIZE - 1, 0);
            int size = (int)(fromPosition - start);

            stream.Seek(start, SeekOrigin.Begin);
            if (stream.Read(buffer, 0, size) == 0) break;

            for (int i = size - 2; i >= 0; i--)
            {
                if (buffer[i] == (byte)'\n')
                    return start + i - (i > 0 && buffer[i - 1] == (byte)'\r' ? 1 : 0);
            }

            fromPosition = start;
        }

        return -1;
    }

    public static (string? line, long position) ReadPreviousLine(string filename, long position)
    {
        filename = AssertFilename(filename);

        long lineStart = -1;
        using (var fs = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            var newlinePos = GetPreviousLinePosition(fs, position);
            if (newlinePos >= 0)
            {
                lineStart = newlinePos;
                fs.Seek(newlinePos, SeekOrigin.Begin);
                if (fs.ReadByte() == (byte)'\r')
                    lineStart++;
            }
        }

        if (lineStart >= 0)
        {
            var lastLine = ReadNextLine(filename, lineStart - 1);
            return lastLine;
        }
        return (null, -1);
    }

    public static (string? line, long position) ReadFirstLine(string filename)
    {
        return ReadNextLine(filename, 0);
    }

    public static (string? line, long position) ReadLastLine(string filename)
    {
        filename = AssertFilename(filename);
        var fileInfo = new FileInfo(filename);
        return ReadPreviousLine(filename, fileInfo.Length - 1);
    }

    private static string AssertFilename(string filename) 
    {
        var fullpath = Path.IsPathRooted(filename) ? filename : Env.ProjectPath(filename);
        if (File.Exists(filename) == false)
        {
            throw new Exception("File " + filename + " not found.");
        }
        return fullpath;
    }

    private static bool IsLineKey(string line)
    {
        var firstSpace = line.IndexOf(' ');
        var c = firstSpace > 0 ? line[firstSpace + 1] : '\0';
        return c == 'K';
    }

    private static string ReverseString(string s)
    {
        char[] a = s.ToCharArray();
        Array.Reverse(a);
        return new string(a);
    }

    private static (string? line, long lineStartPos) ReadLineAt(FileStream fs, long position)
    {
        fs.Seek(position, SeekOrigin.Begin);
        if (position != 0)
        {
            int b;
            while ((b = fs.ReadByte()) != -1)
            {
                if (b == '\n')
                    break;
            }
        }
        long lineStartPos = fs.Position;
        if (lineStartPos >= fs.Length)
            return (null, lineStartPos);
        using (var sr = new StreamReader(fs, Encoding.UTF8, true, BUFFER_SIZE, true))
        {
            var line = sr.ReadLine();
            return (CleanLine(line), lineStartPos);
        }
    }

    private static string? CleanLine(string? input)
    {
        return string.IsNullOrWhiteSpace(input) ? null : input.Replace("\uFEFF", "").Trim();
    }


    private static DateTime ParseTimestamp(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            throw new FormatException("Empty line");
        int spaceIndex = line.IndexOf(' ');
        var timestamp = spaceIndex > 0 ? line.Substring(0, spaceIndex) : line;
        var date = DateUtils.ParseIso(timestamp);
        return date;
    }
}