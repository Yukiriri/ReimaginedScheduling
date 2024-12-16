using ReimaginedScheduling.Shared;
using System;
using System.Threading;
using Windows.System;
using Windows.Win32;

if (args.Length == 2)
{
    
}
else
{
    while (true)
    {
        Console.Write("在游戏窗口内按下Ctrl + PageUp/PageDown转换线程分配\r");
        
        var (windowName, pid, maintid) = ("", 0u, 0u);
        for (; pid == 0; Thread.Sleep(1))
        {
            if ((PInvoke.GetAsyncKeyState((int)VirtualKey.Control) & 0x8000) != 0 &&
                ((PInvoke.GetAsyncKeyState((int)VirtualKey.PageUp) & 0x8000) != 0 || (PInvoke.GetAsyncKeyState((int)VirtualKey.PageDown) & 0x8000) != 0))
            {
                (windowName, pid, maintid) = ProcessInfo.GetFGWindowInfos();

                var processInfo = new ProcessInfo(pid);
                var distribution = ThreadDistributionGenerator.Generate(maintid, processInfo.GetThreadDetailedInfos());
                ThreadDistributionGenerator.ToggleScheduling(pid, distribution, (PInvoke.GetAsyncKeyState((int)VirtualKey.PageUp) & 0x8000) != 0);
                Thread.Sleep(3000);
                Console.Write(Config.ConsoleSplitRow);
            }
        }
    }
}
