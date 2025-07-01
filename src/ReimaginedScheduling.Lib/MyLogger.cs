using System.Text.RegularExpressions;

namespace ReimaginedScheduling.Lib;

public static class MyLogger
{
    private static readonly string fileName = $"{new Regex(@"(?!\"").*(?=.(exe|dll))").Match(Environment.GetCommandLineArgs()[0]).Value}.log";
    private static readonly StreamWriter logger = new(fileName);
    private static string currentLoggerTime => $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}";

    public static void info(string message)
    {
        Console.WriteLine($"[{currentLoggerTime}] {message}");
        debug(message);
    }

    public static void debug(string message)
    {
        logger.WriteLine($"[{currentLoggerTime}] {message}");
        logger.Flush();
    }

}
