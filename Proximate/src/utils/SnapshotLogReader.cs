using System.Text;

namespace Proximate;

public class SnapshotLogReader
{
    public static string? ReadLastLineText(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename))
        {
            throw new ArgumentException("Filename cannot be null or empty.", nameof(filename));
        }

        if (!File.Exists(filename))
        {
            throw new FileNotFoundException("The file does not exist.", filename);
        }

        using (FileStream fs = File.OpenRead(filename))
        {
            if (fs.Length == 0)
            {
                return null;
            }

            var pos = fs.Length;
            byte currentByte;
            while (pos > 0)
            {
                fs.Seek(pos - 1, SeekOrigin.Begin);
                currentByte = (byte)fs.ReadByte();
                if (currentByte != '\n' && currentByte != '\r')
                    break;
                pos--;
            }

            if (pos == 0)
            {
                return null;
            }

            var lineBytes = new List<byte>();
            var buffer = new byte[4096];
            var foundNewline = false;
            while (pos > 0 && !foundNewline)
            {
                var bytesToRead = (int)Math.Min(buffer.Length, pos);
                var blockStart = pos - bytesToRead;
                fs.Seek(blockStart, SeekOrigin.Begin);
                var bytesRead = fs.Read(buffer!, 0, bytesToRead);

                for (int i = bytesRead - 1; i >= 0; i--)
                {
                    currentByte = buffer[i];
                    if (currentByte == '\n' || currentByte == '\r')
                    {
                        pos = blockStart + i;
                        foundNewline = true;
                        break;
                    }
                    else
                    {
                        lineBytes.Add(currentByte);
                    }
                }

                if (!foundNewline)
                {
                    pos -= bytesToRead;
                }
            }
            lineBytes.Reverse();
            return Encoding.UTF8.GetString(lineBytes.ToArray());
        }
    }

    public static Snapshotter.Snapshot? ReadLastSnapshot(string filename)
    {
        var line = ReadLastLineText(filename);
        if (line == null)
            return null;
        var decompressed = Snapshotter.Snapshot.Deserialize(line);
        return decompressed;
    }
}