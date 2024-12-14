using ReimaginedScheduling.Core;
using ReimaginedScheduling.Core.Utils;
using System;
using System.Linq;
using System.Threading;
using Windows.System;
using Windows.Win32;

while (true)
{
    MyConsole.ScrollToTop();
    MyConsole.FillConsole();
    Console.SetCursorPosition(0, 0);
    Console.Write("在游戏窗口内按下Ctrl + PageUp/PageDown转换线程分配");
    
    var (windowName, pid, maintid) = ("", 0u, 0u);
    for (; pid == 0; Thread.Sleep(1))
    {
        if ((PInvoke.GetAsyncKeyState((int)VirtualKey.Control) & 0x8000) != 0)
        {
            if ((PInvoke.GetAsyncKeyState((int)VirtualKey.PageUp) & 0x8000) != 0 || (PInvoke.GetAsyncKeyState((int)VirtualKey.PageDown) & 0x8000) != 0)
            {
                (windowName, pid, maintid) = GameProcessManager.GetForegroundWindowInfos();
                var gtm = new GameThreadManager(pid, maintid);
                gtm.ToggleScheduling((PInvoke.GetAsyncKeyState((int)VirtualKey.PageUp) & 0x8000) != 0);
                Thread.Sleep(2000);
            }
        }
    }
}
