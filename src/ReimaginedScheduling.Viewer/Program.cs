using ReimaginedScheduling.Common;
using ReimaginedScheduling.Common.Tool;
using ReimaginedScheduling.Common.Windows.Info;
using ReimaginedScheduling.Common.Windows.Info.Window;
using System;
using System.Linq;
using System.Threading;
using Windows.System;

ProcessRequire.EnableSeDebug();
ProcessRequire.SetLastCPU();

bool isPaused = false;
bool isCullingRow = true;
bool isSortCycleTime = true;
bool isHideNameless = true;

while (true)
{
    Console.Clear();
    Console.Write(" Ctrl + Ins\r");
    MyHotkey.WaitPress(VirtualKey.Control, VirtualKey.Insert);

    var wi = new MousePointWindowInfo();
    var pid = wi.CurrentPID;
    var maintid = wi.CurrentTID;

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
                case 'h': isHideNameless = !isHideNameless; break;
            }
        }
        if (++updatetime < 10)
            continue;
        if (isPaused)
            continue;
        updatetime = 0;

        Console.WriteLine("\r'q': Quit           | 退出");
        Console.WriteLine("\r'p': Pause          | 暂停");
        Console.WriteLine("\r'c': Culling row    | 剔除显示");
        Console.WriteLine("\r's': Sort CycleTime | 降序CycleTime");
        Console.WriteLine("\r'h': Hide nameless  | 隐藏无名");

        var pi = new ProcessInfo(pid);
        if (pi.IsInvalid || !pi.IsExist())
        {
            pid = 0;
            break;
        }

        var headerstr =    $"|ID    |{"Name",-40}|Priority|{"Mask",16}|CpuSets|Ideal |{"CycleTime",24}|";
        var dataformatstr = "|{0,-6}|{1,-40     }|{2,-8  }|{3,16     }|{4,-7 }|{5,-6}|{6,24          }|\n";
        var headersplitstr = new string('-', headerstr.Length);

        var datastr = string.Format(dataformatstr,
            pi.PID,
            new string([..pi.GetName().Take(40)]),
            pi.CurrentPriority,
            $"{pi.CurrentMask:X}",
            $"{pi.CurrentCpuSets.FirstOrDefault()}({pi.CurrentCpuSetCount})",
            "",
            $"{pi.CurrentCycleTime:N0}") + headersplitstr + '\n';

        var thinfos = pi.GetTIDs().Select(x => new ThreadInfo(x)).Where(x => x.IsValid);
        if (isHideNameless)
            thinfos = thinfos.Where(x => x.TID == maintid || x.CurrentName.Length > 0);
        if (isSortCycleTime)
            thinfos = thinfos.OrderByDescending(x => x.CurrentCycleTime);
        foreach (var thinfo in thinfos)
        {
            datastr += string.Format(dataformatstr,
                thinfo.TID,
                new string([..thinfo.CurrentName.Take(40)]),
                thinfo.CurrentPriority,
                $"{thinfo.CurrentMask:X}",
                $"{thinfo.CurrentCpuSets.FirstOrDefault()}({thinfo.CurrentCpuSetCount})",
                thinfo.CurrentIdealNumber,
                $"{thinfo.CurrentCycleTime:N0}");
        }

        var str = "\n" +
            wi.CurrentName + new string(' ', Math.Max(0, Console.WindowWidth - wi.CurrentName.Length - 1)) + '\n' +
            headersplitstr + '\n' +
            headerstr + '\n' +
            headersplitstr + '\n' +
            datastr +
            headersplitstr;
        foreach (var s in str.Split('\n'))
        {
            if (isCullingRow && Console.CursorTop >= Console.WindowHeight - 1)
                break;
            Console.WriteLine(s);
        }
        MyConsole.FillConsole();
        MyConsole.ScrollToTop();
    }
}
