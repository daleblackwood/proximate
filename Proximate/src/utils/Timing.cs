namespace Proximate;

public static class Timing
{
    public static DateTime Now
    {
        get
        {
            if (Env.IsTestEnvironment && testTimeOverrideValue != null)
                return (DateTime)testTimeOverrideValue;
            return DateTime.UtcNow;
        }
    }

    public static DateTime Epoch => DateTime.UnixEpoch;
    public static DateTime Start { get; private set; }

    public static void Init()
    {
        Start = DateTime.UtcNow;
    }

    public static DateTime Until(TimeSpan span)
    {
        return Now + span;
    }

    public static TimeSpan Since(DateTime other)
    {
        return Now - other;
    }

    public static DateTime Greatest(params DateTime[] dates)
    {
        var result = dates[0];
        for (var i=1; i<dates.Length; i++)
        {
            if (dates[i] > result)
                result = dates[i];
        }
        return result;
    }

    public static bool TryParseTimeSpan(string? input, out TimeSpan parsed)
    {
        parsed = TimeSpan.Zero;
        if (string.IsNullOrWhiteSpace(input))
            return false;
            
        var parts = input.Split(':');
        if (parts.Length < 2 || parts.Length > 3)
            return false;

        int hours = 0, minutes = 0, seconds = 0, milliseconds = 0;
        try
        {
            if (parts.Length == 3)
            {
                hours = int.Parse(parts[0]);
                minutes = int.Parse(parts[1]);
                var secondParts = parts[2].Split('.');
                seconds = int.Parse(secondParts[0]);
                if (secondParts.Length > 1)
                {
                    var ms = secondParts[1].PadRight(3, '0').Substring(0, 3);
                    milliseconds = int.Parse(ms);
                }
            }
            else
            {
                minutes = int.Parse(parts[0]);
                var secondParts = parts[1].Split('.');
                seconds = int.Parse(secondParts[0]);
                if (secondParts.Length > 1)
                {
                    var ms = secondParts[1].PadRight(3, '0').Substring(0, 3);
                    milliseconds = int.Parse(ms);
                }
            }
        }
        catch
        {
            return false;
        }
        parsed = new TimeSpan(0, hours, minutes, seconds, milliseconds);
        return true;
    }


    private static DateTime? testTimeOverrideValue = null;

    public static void TestTimeOverride(DateTime? value)
    {
        if (Env.IsTestEnvironment == false)
            throw new Exception("Test feature only.");
        testTimeOverrideValue = value;
    }
}
