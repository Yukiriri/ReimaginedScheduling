using ReimaginedScheduling.Shared;
using System;
using System.Linq;
using System.Threading;
using Windows.System;
using Windows.Win32;

ProcessRequire.EnableSeDebug();
ProcessRequire.SetLastCPU();
Console.SetWindowSize(Console.WindowWidth + 10, Console.WindowHeight);

bool isPaused = false;
bool isCullingRow = true;
bool isSortCycleTime = true;
bool isHideNameless = true;

void OutConsole(string message = "")
{
    if (isCullingRow)
        MyConsole.FillLineIfFree(message);
    else
        MyConsole.FillLine(message);
}

while (true)
{
    MyConsole.ScrollToTop();
    MyConsole.FillConsole();
    MyConsole.ScrollToTop();
    Console.Write("Ctrl + Ins");

    var pid = 0u;
    var maintid = 0u;
    var windowName = "";
    for (; pid == 0; Thread.Sleep(1))
    {
        if (HotKey.IsCtrl && HotKey.IsKeyDown(VirtualKey.Insert))
        {
            var wi = new WindowInfo(PInvoke.GetForegroundWindow());
            pid = wi.GetPID();
            maintid = wi.GetTID();
            windowName = wi.GetDisplayName(40);
        }
    }

    for (; pid != 0;)
    {
        if (!isPaused)
        {
            Thread.Sleep(1000);
            MyConsole.FillConsole();
            MyConsole.ScrollToTop();

            var pi = new ProcessInfo(pid);
            if (pi.IsInvalid)
            {
                pid = 0;
                break;
            }

            var headerstr = $"|ID   |{"Name",-40}|Priority|{"Mask",-16}|CpuSets|CpuSetMasks|{"CycleTime",-21}|Ideal|";
            var headersplitstr = new string('-', headerstr.Length);
            var datastr = "";
            MyConsole.FillLine(headersplitstr);
            MyConsole.FillLine(headerstr);
            MyConsole.FillLine(headersplitstr);

            datastr  = $"|{pid,-5}";
            datastr += $"|{windowName}";
            datastr += $"|{pi.CurrentPriority,-8}";
            datastr += $"|{pi.CurrentMask,-16:X}";
            datastr += $"|{$"({pi.CurrentCpuSetCount})",-7}";
            datastr += $"|{$"({pi.CurrentCpuSetMaskCount})",-11}";
            datastr += $"|{pi.CurrentCycleTime,-21}";
            datastr += $"|{"",-5}";
            datastr += "|";
            Console.Write(datastr + new string(' ', Console.WindowWidth - headersplitstr.Length));
            MyConsole.FillLine(headersplitstr);
            
            var thinfos = ProcessInfo.GetTIDs(pid).Select(x => (TID: x, ti: new ThreadInfo(x)));
            if (isSortCycleTime)
                thinfos = thinfos.OrderByDescending(x => x.ti.CurrentCycleTime);
            foreach (var thinfo in thinfos)
            {
                if (thinfo.ti.IsValid)
                {
                    var tname = thinfo.ti.CurrentName;
                    if (isHideNameless && thinfo.TID != maintid && tname.Length == 0)
                        continue;
                    tname = tname[0..Math.Min(tname.Length, 40)];
                    
                    datastr  = $"|{thinfo.TID,-5}";
                    datastr += $"|{tname,-40}";
                    datastr += $"|{thinfo.ti.CurrentPriority,-8}";
                    datastr += $"|{thinfo.ti.CurrentMask,-16:X}";
                    datastr += $"|{$"{thinfo.ti.CurrentCpuSets.FirstOrDefault()}({thinfo.ti.CurrentCpuSetCount})",-7}";
                    datastr += $"|{$"({thinfo.ti.CurrentCpuSetMaskCount})",-11}";
                    datastr += $"|{thinfo.ti.CurrentCycleTime,-21}";
                    datastr += $"|{thinfo.ti.CurrentIdealNumber,-5}";
                    datastr += "|";
                    OutConsole(datastr);
                }
            }
            if (thinfos.Any())
                OutConsole(headersplitstr);
            OutConsole();
            OutConsole("'q': Quit");
            OutConsole("'p': Pause");
            OutConsole("'c': Culling Row");
            OutConsole("'s': Sort CycleTime");
            OutConsole("'n': Toggle Nameless");
            OutConsole();
        }

        while (Console.KeyAvailable)
        {
            switch (Console.ReadKey().KeyChar)
            {
                case 'q': pid = 0;
                    break;
                case 'p': isPaused = !isPaused;
                    break;
                case 'c': isCullingRow = !isCullingRow;
                    break;
                case 's': isSortCycleTime = !isSortCycleTime;
                    break;
                case 'n': isHideNameless = !isHideNameless;
                    break;
            }
        }
    }
}
