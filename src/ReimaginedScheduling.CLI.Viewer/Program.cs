using ReimaginedScheduling.Common;
using ReimaginedScheduling.Common.Tool;
using ReimaginedScheduling.Common.Windows.Device;
using ReimaginedScheduling.Common.Windows.Info;
using ReimaginedScheduling.Common.Windows.Info.Window;
using System;
using System.Linq;
using System.Threading;
using Windows.System;

ProcessRequire.EnableSeDebug();
ProcessRequire.SetLastCPU();
// Console.SetWindowSize(Console.WindowWidth + 10, Console.WindowHeight);

bool isPaused = false;
bool isCullingRow = true;
bool isSortCycleTime = true;
bool isHideNameless = true;

while (true)
{
    Console.Clear();
    Console.Write(" Ctrl + Ins\r");

    for (; !HotKey.IsKeyDown([VirtualKey.Control, VirtualKey.Insert]); Thread.Sleep(1));
    var wi = new MousePointWindowInfo();
    var pid = wi.CurrentPID;
    var maintid = wi.CurrentTID;
    var windowName = wi.GetDisplayName(40);

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
        #if !DEBUG
        GC.Collect();
        #endif

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

        var headerstr = $"|   ID|{"Name",-40}|Priority|{"Mask",16}|CpuSets|Ideal|{"CycleTime",24}|";
        var headersplitstr = new string('-', headerstr.Length);
        
        var procstr = "";
        procstr += $"|{pid,5}";
        procstr += $"|{windowName}";
        procstr += $"|{pi.CurrentPriority,8}";
        procstr += $"|{pi.CurrentMask,16:X}";
        procstr += $"|{$"({pi.CurrentCpuSetCount})",7}";
        procstr += $"|{"",5}";
        procstr += $"|{pi.CurrentCycleTime,24:N0}";
        procstr += "|\n";
        
        var thstr = "";
        var thinfos = ProcessInfo.GetTIDs(pid).Select(x => new ThreadInfo(x)).Where(x => x.IsValid);
        if (isHideNameless)
            thinfos = thinfos.Where(x => x.TID == maintid || x.CurrentName.Length > 0);
        if (isSortCycleTime)
            thinfos = thinfos.OrderByDescending(x => x.CurrentCycleTime);
        foreach (var thinfo in thinfos)
        {
            thstr += $"|{thinfo.TID,5}";
            thstr += $"|{new string([..thinfo.CurrentName.Take(40)]),-40}";
            thstr += $"|{thinfo.CurrentPriority,8}";
            thstr += $"|{thinfo.CurrentMask,16:X}";
            thstr += $"|{$"{thinfo.CurrentCpuSets.FirstOrDefault()}({thinfo.CurrentCpuSetCount})",7}";
            thstr += $"|{thinfo.CurrentIdealNumber,5}";
            thstr += $"|{thinfo.CurrentCycleTime,24:N0}";
            thstr += "|\n";
        }

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
            if (isCullingRow && Console.CursorTop >= Console.WindowHeight - 1)
                continue;
            Console.WriteLine(s);
        }
        MyConsole.FillConsole();
        MyConsole.ScrollToTop();
    }
}
