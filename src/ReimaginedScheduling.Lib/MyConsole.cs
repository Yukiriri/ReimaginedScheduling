
namespace ReimaginedScheduling.Lib;

public static class MyConsole
{
    public static void scrollToTop()
    {
        Console.SetWindowPosition(0, 0);
        Console.SetCursorPosition(0, 0);
    }

    public static void fillSpace()
    {
        var fill_count = Console.WindowWidth * (Console.WindowHeight - Console.CursorTop - 1) + Console.WindowWidth - Console.CursorLeft - 1;
        if (fill_count > 0)
            Console.Write(new string(' ', fill_count));
    }
}
