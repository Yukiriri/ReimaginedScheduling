using System;

namespace ReimaginedScheduling.Shared;

public class MyConsole
{
    public static void FillLine(string message = "")
    {
        Console.Write(message);
        if (message.Length < Console.WindowWidth)
            Console.Write(new string(' ', Console.WindowWidth - message.Length));
    }

    public static void FillLineIfFree(string message = "")
    {
        var (_, Top) = Console.GetCursorPosition();
        if (Top < Console.WindowHeight - 1)
            FillLine(message);
    }

    public static void FillConsole()
    {
        var (Left, Top) = Console.GetCursorPosition();
        var fillcount = Console.WindowWidth * (Console.WindowHeight - Top - 1) + Console.WindowWidth - Left - 1;
        if (fillcount > 0)
            Console.Write(new string(' ', fillcount));
    }

    public static void ScrollToTop()
    {
        Console.SetWindowPosition(0, 0);
        Console.SetCursorPosition(0, 0);
    }
}
