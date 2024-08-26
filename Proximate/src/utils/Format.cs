namespace Proximate;

public static class Format
{
    public static string DateTime(DateTime? dateTime)
    {
        return dateTime?.ToString("o") ?? "";
    }
}