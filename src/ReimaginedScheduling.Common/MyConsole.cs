using System;

namespace ReimaginedScheduling.Common;

public class MyConsole
{
    public static void ScrollToTop()
    {
        Console.SetWindowPosition(0, 0);
        Console.SetCursorPosition(0, 0);
    }

    public static void FillConsole()
    {
        var (Left, Top) = Console.GetCursorPosition();
        var fillcount = Console.WindowWidth * (Console.WindowHeight - Top - 1) + Console.WindowWidth - Left - 1;
        if (fillcount > 0)
            Console.Write(new string(' ', fillcount));
    }
}
