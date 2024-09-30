using System.Diagnostics;

namespace Proximate;

public static class Log
{
    public enum LogLevel
    {
        Debug = 0,
        Info,
        Warning,
        Error
    }

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

    private static readonly LogLevel RecordLevel = LogLevel.Warning;

    public static void LogLine(string? s, LogLevel level)
    {
        var prefix = level >= LogLevel.Warning ? Enum.GetName(level) + ": " : "";
        Console.WriteLine(prefix + s);
        if (level >= RecordLevel && SentrySdk.IsEnabled && string.IsNullOrEmpty(s) == false)
        {
            var sentryLevel = SentryLevel.Info;
            if (level >= LogLevel.Error)
            {
                sentryLevel = SentryLevel.Error;
            }
            if (level >= LogLevel.Warning)
            {
                sentryLevel = SentryLevel.Warning;
            }
            SentrySdk.CaptureMessage(s, sentryLevel);
        }
    }

    public static void Debug(string? s) => LogLine(s, LogLevel.Debug);

    public static void Info(string? s) => LogLine(s, LogLevel.Info);

     public static void Warn(string? s) => LogLine(s, LogLevel.Warning);

    public static void Error(string? s) => LogLine(s, LogLevel.Error);

    public static void Error(Exception e) => LogLine(e.Message, LogLevel.Error);

    public static double TimingBegin() => (Timing.Now - Timing.Epoch).TotalSeconds;

    public static void TimingWarn(double beginTime, double maxDuration, string operation, LogLevel level = LogLevel.Warning)
    {
        var duration = TimingBegin() - beginTime;
        if (duration > maxDuration)
        {
            var msg = string.Format("%s exceeded max duration %ds: took %ds", operation, maxDuration, duration);
            LogLine(msg, level);
        }
    }
}