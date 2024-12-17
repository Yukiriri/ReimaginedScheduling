using System;
using System.IO;

namespace ReimaginedScheduling.Shared;

public class MyLogger
{
    public static void Debug(string message)
    {
        _logger.WriteLine($"[{NextLoggerTime()}] {message}");
        _logger.Flush();
    }

    public static void Info(string message)
    {
        Console.WriteLine($"[{NextLoggerTime()}] {message}");
        Debug(message);
    }

    private static string NextLoggerTime() => $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}";

    private static readonly StreamWriter _logger = new($"ReimagedScheduling.log");
}