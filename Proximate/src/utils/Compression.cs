namespace Proximate;
using System.IO;
using System.IO.Compression;
using System.Text;

public static class Compression
{
    private const string PREFIX = "GZ1:";

    public static string Compress(string value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        var inputBytes = Encoding.UTF8.GetBytes(value);

        using (var outputStream = new MemoryStream())
        {
            using (var brotliStream = new GZipStream(outputStream, CompressionLevel.Optimal, true))
            {
                brotliStream.Write(inputBytes, 0, inputBytes.Length);
            }
            var compressedBytes = outputStream.ToArray();
            return PREFIX + Convert.ToBase64String(compressedBytes);
        }
    }

    public static string Decompress(string value)
    {
        if (!IsCompressed(value)) return value;

        var base64Data = value.Substring(PREFIX.Length);
        var compressedBytes = Convert.FromBase64String(base64Data);

        using (var inputStream = new MemoryStream(compressedBytes))
        using (var brotliStream = new GZipStream(inputStream, CompressionMode.Decompress))
        using (var outputStream = new MemoryStream())
        {
            brotliStream.CopyTo(outputStream);
            return Encoding.UTF8.GetString(outputStream.ToArray());
        }
    }

    public static bool IsCompressed(string value)
    {
        return value.StartsWith(PREFIX);
    }
}