using ReimaginedScheduling.Shared;
using System;
using System.Linq;
using System.Threading;
using Windows.System;
using Windows.Win32;

ProcessUtilities.EnableSeDebug();
ProcessUtilities.SetLastCPU();
Console.SetWindowSize(Console.WindowWidth + 10, Console.WindowHeight);

bool isPaused = false;
bool isCullingRow = true;
bool isSortCycleTime = true;
bool isHideNameless = true;

while (true)
{
    Console.Clear();
    Console.Write("Ctrl + Ins \r");

    var pid = 0u;
    var maintid = 0u;
    var windowName = "";
    for (; pid == 0; Thread.Sleep(1))
    {
        if (HotKey.IsCtrl && HotKey.IsKeyDown(VirtualKey.Insert))
        {
            var wi = new WindowInfo(PInvoke.GetForegroundWindow());
            pid = wi.CurrentPID;
            maintid = wi.CurrentTID;
            windowName = wi.GetDisplayName(40);
        }
    }

    for (var updatetime = 0; pid != 0;)
    {
        Thread.Sleep(100);
        while (Console.KeyAvailable)
        {
            switch (Console.ReadKey().KeyChar)
            {
                case 'q': pid = 0; break;
                case 'p': isPaused = !isPaused; break;
                case 'c': isCullingRow = !isCullingRow; break;
                case 's': isSortCycleTime = !isSortCycleTime; break;
                case 'n': isHideNameless = !isHideNameless; break;
            }
        }
        if (++updatetime < 10)
            continue;
        if (isPaused)
            continue;
        updatetime = 0;

        Console.WriteLine("\r'q': Quit");
        Console.WriteLine("\r'p': Pause");
        Console.WriteLine("\r'c': Culling row");
        Console.WriteLine("\r's': Sort CycleTime");
        Console.WriteLine("\r'h': Hide Nameless");
        
        var pi = new ProcessInfo(pid);
        if (pi.IsInvalid)
        {
            pid = 0;
            break;
        }

        var procstr = "";
        procstr += $"|{pid,-5}";
        procstr += $"|{windowName}";
        procstr += $"|{pi.CurrentPriority,-8}";
        procstr += $"|{pi.CurrentMask,-16:X}";
        procstr += $"|{$"({pi.CurrentCpuSetCount})",-7}";
        procstr += $"|{$"({pi.CurrentCpuSetMaskCount})",-11}";
        procstr += $"|{pi.CurrentCycleTime,-21}";
        procstr += $"|{"",-5}";
        procstr += "|\n";
        
        var thstr = "";
        var thinfos = ProcessUtilities.GetTIDs(pid)
            .Select(x => (TID: x, ti: new ThreadInfo(x)))
            .Where(x => x.ti.IsValid);
        if (isHideNameless)
            thinfos = thinfos.Where(x => x.TID == maintid || x.ti.CurrentName.Length > 0);
        if (isSortCycleTime)
            thinfos = thinfos.OrderByDescending(x => x.ti.CurrentCycleTime);
        foreach (var thinfo in thinfos)
        {
            thstr += $"|{thinfo.TID,-5}";
            thstr += $"|{new string([..thinfo.ti.CurrentName.Take(40)]),-40}";
            thstr += $"|{thinfo.ti.CurrentPriority,-8}";
            thstr += $"|{thinfo.ti.CurrentMask,-16:X}";
            thstr += $"|{$"{thinfo.ti.CurrentCpuSets.FirstOrDefault()}({thinfo.ti.CurrentCpuSetCount})",-7}";
            thstr += $"|{$"({thinfo.ti.CurrentCpuSetMaskCount})",-11}";
            thstr += $"|{thinfo.ti.CurrentCycleTime,-21}";
            thstr += $"|{thinfo.ti.CurrentIdealNumber,-5}";
            thstr += "|\n";
        }

        var headerstr = $"|ID   |{"Name",-40}|Priority|{"Mask",-16}|CpuSets|CpuSetMasks|{"CycleTime",-21}|Ideal|";
        var headersplitstr = new string('-', headerstr.Length);
        var str = 
            headersplitstr + '\n' +
            headerstr + '\n' +
            headersplitstr + '\n' +
            procstr +
            headersplitstr + '\n' +
            thstr +
            headersplitstr;
        foreach (var s in str.Split('\n'))
        {
            if (isCullingRow && Console.CursorTop >= Console.WindowHeight)
                continue;
            Console.WriteLine(s);
        }
        MyConsole.FillConsole();
        MyConsole.ScrollToTop();
    }
}
