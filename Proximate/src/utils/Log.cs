namespace Proximate;

public static class Log
{
    public static void Info(string? s)
    {
        Console.WriteLine(s);
    }

    public static void Error(string? s)
    {
        Console.WriteLine("ERROR: " + s);
    }
}