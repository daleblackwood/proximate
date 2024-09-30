namespace Proximate;

public static class DateUtils
{
    public static string Format(DateTime? dateTime)
    {
        return dateTime?.ToString("o") ?? "";
    }
}

public static class DateTimeExtensions
{
    public static DateTime RoundUp(this DateTime dateTime, TimeSpan timeSpan)
    {
        long delta = (long)(timeSpan.TotalMilliseconds - (dateTime.Ticks % timeSpan.Ticks)) % timeSpan.Ticks;
        return dateTime.AddTicks(delta);
    }
}