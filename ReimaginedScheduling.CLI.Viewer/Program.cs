using ReimaginedScheduling.Shared;
using System;
using System.Linq;
using System.Threading;

Console.SetWindowSize(Console.WindowWidth + 10, Console.WindowHeight);
ProcessRequire.EnableSeDebug();
ProcessRequire.SetLastCpu();

bool isSortCycleTime = true;
bool isHideNameless = true;

while (true)
{
    MyConsole.ScrollToTop();
    MyConsole.FillConsole();
    Console.SetCursorPosition(0, 0);
    Console.Write("Ctrl + Ins");

    var pid = 0u;
    var windowName = "";
    var maintid = 0u;
    for (; pid == 0; Thread.Sleep(1))
    {
        if (HotKey.IsCtrl && HotKey.IsInsert)
        {
            var wi = new WindowInfo();
            wi.SetForegroundHWND();
            pid = wi.GetPID();
            windowName = wi.GetDisplayName(40);
            maintid = wi.GetTID();
        }
    }

    for (; pid != 0;)
    {
        MyConsole.ScrollToTop();
        Thread.Sleep(500);
        
        var pi = new ProcessInfo(pid);
        if (pi.IsValid)
        {
            var str = $"|PID  |{"Name",-40}|Priority|{"Mask",-16}|CpuSets|CpuSetMasks|MainTID|";
            var splitstr = new string('-', str.Length);
            MyConsole.FillLineIfFree(splitstr);
            MyConsole.FillLineIfFree(str);
            MyConsole.FillLineIfFree(splitstr);

            str  = $"|{pid,-5}";
            str += $"|{windowName}";
            str += $"|{pi.GetPriority(),-8}";
            str += $"|{pi.GetMask(),-16:X}";
            str += $"|{$"({pi.GetCpuSets().Length})",-7}";
            str += $"|{$"({pi.GetCpuSetMaskCount()})",-11}";
            str += $"|{maintid,-7}";
            str += "|";
            Console.Write(str + new string(' ', Math.Max(0, Console.WindowWidth - splitstr.Length)));
            MyConsole.FillLineIfFree(splitstr);
        }
        else
        {
            pid = 0;
            break;
        }
        
        {
            var str = $"|TID  |{"Name",-40}|Priority|{"Mask",-16}|CpuSets|CpuSetMasks|Ideal|{"CycleTime",-21}|";
            var splitstr = new string('-', str.Length);
            MyConsole.FillLineIfFree(splitstr);
            MyConsole.FillLineIfFree(str);
            MyConsole.FillLineIfFree(splitstr);

            var ticts = ThreadInfo.PackWithCycleTime(ProcessInfo.GetTIDs(pid));
            if (isSortCycleTime)
                ticts = [..ticts.OrderByDescending(x => x.CycleTime)];
            foreach (var tict in ticts)
            {
                var ti = new ThreadInfo(tict.TID);
                if (ti.IsValid)
                {
                    var tname = ti.GetName();
                    if (isHideNameless && tict.TID != maintid && tname.Length == 0)
                        continue;
                    str  = $"|{tict.TID,-5}";
                    str += $"|{tname[0..Math.Min(tname.Length, 40)],-40}";
                    str += $"|{ti.GetPriority(),-8}";
                    str += $"|{ti.GetMask(),-16:X}";
                    str += $"|{$"{ti.GetCpuSets().DefaultIfEmpty().First()}({ti.GetCpuSets().Length})",-7}";
                    str += $"|{$"({ti.GetCpuSetMaskCount()})",-11}";
                    str += $"|{ti.GetIdealNumber(),-5}";
                    str += $"|{tict.CycleTime,-21}";
                    str += "|";
                    MyConsole.FillLineIfFree(str);
                }
            }
            MyConsole.FillLineIfFree(splitstr);
        }

        MyConsole.FillLineIfFree();
        MyConsole.FillLineIfFree("'Q': Quit");
        MyConsole.FillLineIfFree("'S': Sort CycleTime");
        MyConsole.FillLineIfFree("'N': Toggle Nameless");
        MyConsole.FillConsole();
        while (Console.KeyAvailable)
        {
            switch (Console.ReadKey().KeyChar)
            {
                case 'q':
                    pid = 0;
                    break;
                case 's':
                    isSortCycleTime = !isSortCycleTime;
                    break;
                case 'n':
                    isHideNameless = !isHideNameless;
                    break;
            }
        }
    }
}
