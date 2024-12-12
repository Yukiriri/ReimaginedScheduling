using System;

namespace ReimaginedScheduling.Services.Utils;

public class MyConsole
{
    public static void FillLine(string msg)
    {
        Console.Write(msg);
        if (msg.Length < Console.WindowWidth)
            Console.Write(new string(' ', Console.WindowWidth - msg.Length));
    }

    public static void FillConsole()
    {
        var (Left, Top) = Console.GetCursorPosition();
        var fillcount = Console.WindowWidth * (Console.WindowHeight - Top - 1) + Console.WindowWidth - Left - 2;
        if (fillcount > 0)
            Console.Write(new string(' ', fillcount));
    }
}
