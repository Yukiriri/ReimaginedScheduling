using System;
using System.IO;
using System.Text.RegularExpressions;

namespace ReimaginedScheduling.Common;

public class MyLogger
{
    public static void Debug(string message)
    {
        _logger.WriteLine($"[{CurrentLoggerTime}] {message}");
        _logger.Flush();
    }

    public static void Info(string message)
    {
        Console.WriteLine($"[{CurrentLoggerTime}] {message}");
        Debug(message);
    }

    private static string CurrentLoggerTime => $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}";

    private static readonly string CurrentLoggerName = new Regex(@"(?!.*\\).*(?=.(exe|dll))").Match(Environment.GetCommandLineArgs()[0]).Value;

    private static readonly StreamWriter _logger = new($"{CurrentLoggerName}.log");
}
