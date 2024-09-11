using Swan;

namespace Proximate;

public static class Timing
{
    public static DateTime Now
    {
        get
        {
            #if TESTING
                if (testTimeOverrideValue != null)
                    return testTimeOverrideValue;
            #endif
            return DateTime.UtcNow;
        }
    }

    public static DateTime Epoch => DateTime.UnixEpoch;

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

#if TESTING
    private static DateTime? testTimeOverrideValue = null;

    public static void TestTimeOverride(DateTime? value)
    {
        testTimeOverrideValue = value;
    }
#endif
}
