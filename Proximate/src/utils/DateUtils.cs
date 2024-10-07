using System.Globalization;

namespace Proximate;

public static class DateUtils
{
    public static string Format(DateTime? dateTime)
    {
        return dateTime?.ToString("o") ?? "";
    }
    
    public static DateTime? TryParseIso(string timestamp)
    {
        var cleaned = timestamp.Trim('\uFEFF', ' ', '\t', '\n', '\r');
        if (DateTime.TryParse(cleaned, out var result))
        {
            if (result.Kind != DateTimeKind.Utc)
            {
                result = result.ToUniversalTime();
            }
            return result;
        }
        return null;
    }

    public static DateTime ParseIso(string timestamp) => TryParseIso(timestamp) ?? throw new FormatException($"Invalid ISO 8601 date format: {timestamp}");
}

public static class DateTimeExtensions
{
    public static DateTime RoundUp(this DateTime dateTime, TimeSpan timeSpan)
    {
        long delta = (long)(timeSpan.TotalMilliseconds - (dateTime.Ticks % timeSpan.Ticks)) % timeSpan.Ticks;
        return dateTime.AddTicks(delta);
    }
}