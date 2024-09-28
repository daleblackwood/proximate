namespace Proximate;

public static class Log
{
    public static void Init(string environmentName)
    {
        if (Env.IsProduction)
        {
            SentrySdk.Init(options => {
                options.Dsn = "https://d850786f0dd55f313242ba791608989f@o4508956393340928.ingest.us.sentry.io/4508956398256128";
                options.Debug = false;
                options.AutoSessionTracking = true;
                options.Environment = environmentName;
            });
        }
    }

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

     public static void Warn(string? s)
    {
        Console.WriteLine("WARNING: " + s);
        if (SentrySdk.IsEnabled)
        {
            SentrySdk.CaptureMessage(s ?? "Unknown Warning", SentryLevel.Warning);
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