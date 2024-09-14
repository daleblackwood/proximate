namespace Proximate;

public static class Log
{
    public static void Verbose(string? s)
    {
        Console.WriteLine(s);
    }

    public static void Info(string? s)
    {
        Console.WriteLine(s);
        if (SentrySdk.IsEnabled && string.IsNullOrEmpty(s) == false)
        {
            SentrySdk.CaptureMessage(s, SentryLevel.Info);
        }
    }

    public static void Error(string? s)
    {
        Console.WriteLine("ERROR: " + s);
        if (SentrySdk.IsEnabled)
        {
            SentrySdk.CaptureMessage(s ?? "Unknown Error", SentryLevel.Error);
        }
    }

    public static void Error(Exception e)
    {
        Console.WriteLine("ERROR: EXCEPTION: " + e.Message);
        if (SentrySdk.IsEnabled)
        {
            SentrySdk.CaptureException(e);
        }
    }
}