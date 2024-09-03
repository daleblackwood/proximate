using Xunit;

namespace Proximate.Tests;

public static class AssertUtils
{
    public static void IsTimeWithin(DateTime a, DateTime b, int maxMilliseconds, string message = "")
    {
        var diffMs = (a - b).TotalMilliseconds;
        if (Math.Abs(diffMs) > maxMilliseconds)
        {
            var prefix = string.IsNullOrEmpty(message) ? "" : $"{message}: ";
            Assert.Fail($"{prefix}{a} was not within {maxMilliseconds}ms of {b}, is {diffMs}ms");
        }
    }

    public static void ThrowSync(Action action, string message = "")
    {
        var threw = false;
        try 
        {
            action.Invoke();
        }
        catch
        {
            threw = true;
        }
        Assert.True(threw, string.IsNullOrEmpty(message) ? "method should have thrown" : message);
    }
}