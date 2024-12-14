using ReimaginedScheduling.Core;
using ReimaginedScheduling.Core.Utils;
using System;
using System.Linq;
using System.Threading;
using Windows.System;
using Windows.Win32;

bool isHideNameless = true;
while (true)
{
    MyConsole.ScrollToTop();
    MyConsole.FillConsole();
    Console.SetCursorPosition(0, 0);
    Console.Write("在游戏窗口内按下Ctrl + Ins开始读取线程");

    var (windowName, pid, maintid) = ("", 0u, 0u);
    for (; pid == 0; Thread.Sleep(1))
    {
        if ((PInvoke.GetAsyncKeyState((int)VirtualKey.Control) & 0x8000) != 0 &&
            (PInvoke.GetAsyncKeyState((int)VirtualKey.Insert) & 0x8000) != 0)
        {
            (windowName, pid, maintid) = GameProcessManager.GetForegroundWindowInfos();
        }
    }

    for (; pid != 0;)
    {
        Thread.Sleep(500);
        var gtm = new GameThreadManager(pid, maintid);
        {
            var pdi = gtm.GetProcessDetailedInfo();
            if (pdi == null)
            {
                pid = 0;
                break;
            }
            
            var str = $"|{"Name",-40}|PID  |Priority|{"Mask",-16}|CpuSets|CpuSetMasks|MainTID|";
            var splitstr = new string('-', str.Length);
            MyConsole.FillLine(splitstr);
            MyConsole.FillLine(str);
            MyConsole.FillLine(splitstr);

            var showlength = 0;
            while ((showlength = windowName.Aggregate(0, (length, next) => length + (next > 127 ? 2 : 1))) > 40)
            {
                windowName = windowName[0..(windowName.Length - 1)];
            }
            windowName += new string(' ', 40 - showlength);
            str  = $"|{windowName}";
            str += $"|{pid,-5}";
            str += $"|{pdi.Value.Priority,-8}";
            str += $"|{pdi.Value.Mask,-16:X}";
            str += $"|{$"({pdi.Value.CpuSetsCount})",-7}";
            str += $"|{$"({pdi.Value.CpuSetMasksCount})",-11}";
            str += $"|{maintid,-7}";
            str += "|";
            Console.Write(str + new string(' ', Math.Max(0, Console.WindowWidth - splitstr.Length)));
            MyConsole.FillLine(splitstr);
            MyConsole.FillLine();
            MyConsole.FillLine();
        }
        {
            var str = $"|{"Name",-40}|TID  |Priority|{"Mask",-16}|CpuSets|CpuSetMasks|Ideal|";
            var splitstr = new string('-', str.Length);
            MyConsole.FillLine(splitstr);
            MyConsole.FillLine(str);
            MyConsole.FillLine(splitstr);

            foreach (var tdi in gtm.GetThreadDetailedInfos())
            {
                if (isHideNameless && tdi.TID != maintid && tdi.Name.Length == 0)
                    continue;
                str  = $"|{tdi.Name[0..Math.Min(tdi.Name.Length, 40)],-40}";
                str += $"|{tdi.TID,-5}";
                str += $"|{tdi.Priority,-8}";
                str += $"|{tdi.Mask,-16:X}";
                str += $"|{$"{tdi.CpuSetID}({tdi.CpuSetsCount})",-7}";
                str += $"|{$"({tdi.CpuSetMasksCount})",-11}";
                str += $"|{tdi.Ideal,-5}";
                str += "|";
                MyConsole.FillLine(str);
            }
            MyConsole.FillLine(splitstr);
        }

        MyConsole.FillLine();
        MyConsole.FillLine("'Q': quit");
        MyConsole.FillLine("'N': toggle nameless");
        MyConsole.FillConsole();
        MyConsole.ScrollToTop();

        while (Console.KeyAvailable)
        {
            switch (Console.ReadKey().KeyChar)
            {
                case 'q':
                    pid = 0;
                    break;
                case 'n':
                    isHideNameless = !isHideNameless;
                    break;
            }
        }
    }
}
